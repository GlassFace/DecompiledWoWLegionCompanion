using bgs;
using Blizzard;
using bnet.protocol;
using bnet.protocol.account;
using bnet.protocol.attribute;
using bnet.protocol.game_utilities;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using JamLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using WowJamMessages;
using WowJamMessages.JSONRealmList;
using WowJamMessages.MobileClientJSON;
using WowJamMessages.MobileJSON;
using WowJamMessages.MobilePlayerJSON;
using WowStatConstants;

public class Login : MonoBehaviour
{
	private class LoginGameAccount
	{
		public EntityId gameAccount;

		public string name;

		public bool isBanned;

		public bool isSuspended;
	}

	public enum eLoginState
	{
		IDLE = 0,
		WAIT_FOR_ASSET_BUNDLES = 1,
		WEB_AUTH_START = 2,
		WEB_AUTH_LOADING = 3,
		WEB_AUTH_IN_PROGRESS = 4,
		WEB_AUTH_FAILED = 5,
		BN_LOGIN_START = 6,
		BN_LOGIN_WAIT_FOR_LOGON = 7,
		BN_LOGIN_PROVIDE_TOKEN = 8,
		BN_LOGGING_IN = 9,
		BN_ACCOUNT_NAME_WAIT = 10,
		BN_TICKET_WAIT = 11,
		BN_SUBREGION_LIST_WAIT = 12,
		BN_CHARACTER_LIST_WAIT = 13,
		BN_REALM_JOIN_WAIT = 14,
		BN_LOGIN_UNKNOWN = 15,
		MOBILE_CONNECT = 16,
		MOBILE_CONNECTING = 17,
		MOBILE_CONNECT_FAILED = 18,
		MOBILE_DISCONNECTING = 19,
		MOBILE_DISCONNECTED = 20,
		MOBILE_DISCONNECTED_BY_SERVER = 21,
		MOBILE_DISCONNECTED_IDLE = 22,
		MOBILE_LOGGING_IN = 23,
		MOBILE_LOGGED_IN = 24,
		MOBILE_LOGGED_IN_IDLE = 25
	}

	private class GameAccountStateCallback
	{
		private EntityId m_entityID;

		public EntityId EntityID
		{
			get
			{
				return this.m_entityID;
			}
			set
			{
				this.m_entityID = value;
			}
		}

		public void Callback(RPCContext context)
		{
			Login.instance.LoginLog("GameAccountStateCallback called");
			GetGameAccountStateResponse getGameAccountStateResponse = GetGameAccountStateResponse.ParseFrom(context.Payload);
			Login.instance.LoginLog(string.Concat(new object[]
			{
				"GameAccountStateCallback: Received name ",
				getGameAccountStateResponse.State.GameLevelInfo.Name,
				" for game account ",
				this.EntityID.Low
			}));
			if (getGameAccountStateResponse.State.GameStatus.IsBanned)
			{
				Login.instance.LoginLog(string.Concat(new object[]
				{
					"GameAccountStateCallback: Account ",
					getGameAccountStateResponse.State.GameLevelInfo.Name,
					", (",
					this.EntityID.Low,
					") is Banned!"
				}));
			}
			if (getGameAccountStateResponse.State.GameStatus.IsSuspended)
			{
				Login.instance.LoginLog(string.Concat(new object[]
				{
					"GameAccountStateCallback: Account ",
					getGameAccountStateResponse.State.GameLevelInfo.Name,
					", (",
					this.EntityID.Low,
					") is Suspended!"
				}));
			}
			Login.instance.AddGameAccountButton(this.EntityID, getGameAccountStateResponse.State.GameLevelInfo.Name, getGameAccountStateResponse.State.GameStatus.IsBanned, getGameAccountStateResponse.State.GameStatus.IsSuspended);
		}
	}

	private class CharacterListCallback
	{
		private string m_subRegion;

		private JSONRealmListUpdates m_updates;

		private JSONRealmCharacterList m_characters;

		public string SubRegion
		{
			get
			{
				return this.m_subRegion;
			}
			set
			{
				this.m_subRegion = value;
			}
		}

		public void Callback(RPCContext context)
		{
			Login.instance.LoginLog("--------- CharacterListCallback received for subregion " + this.SubRegion + " ----------");
			ClientResponse clientResponseFromContext = GamesAPI.GetClientResponseFromContext(context);
			if (clientResponseFromContext != null)
			{
				if (Login.instance.m_clearCharacterListOnReply)
				{
					Login.instance.m_clearCharacterListOnReply = false;
					AllPanels.instance.ClearCharacterList();
				}
				using (List<Attribute>.Enumerator enumerator = clientResponseFromContext.AttributeList.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Attribute current = enumerator.get_Current();
						if (current.Name == "Param_RealmList")
						{
							string value = Login.DecompressJsonAttribBlob(current.Value.BlobValue);
							this.m_updates = JsonConvert.DeserializeObject<JSONRealmListUpdates>(value);
							JamJSONRealmListUpdatePart[] updates = this.m_updates.Updates;
							for (int i = 0; i < updates.Length; i++)
							{
								JamJSONRealmListUpdatePart jamJSONRealmListUpdatePart = updates[i];
								Login.instance.LoginLog(string.Concat(new object[]
								{
									"Found realm named ",
									jamJSONRealmListUpdatePart.Update.Name,
									", subregion ",
									this.SubRegion,
									", addr ",
									jamJSONRealmListUpdatePart.Update.WowRealmAddress
								}));
							}
						}
						else if (current.Name == "Param_CharacterList")
						{
							string value2 = Login.DecompressJsonAttribBlob(current.Value.BlobValue);
							this.m_characters = JsonConvert.DeserializeObject<JSONRealmCharacterList>(value2);
						}
					}
				}
				string @string = SecurePlayerPrefs.GetString("CharacterID", Main.uniqueIdentifier);
				if (this.m_characters != null && this.m_updates != null)
				{
					JamJSONCharacterEntry[] characterList = this.m_characters.CharacterList;
					for (int j = 0; j < characterList.Length; j++)
					{
						JamJSONCharacterEntry jamJSONCharacterEntry = characterList[j];
						bool flag = false;
						JamJSONRealmListUpdatePart[] updates2 = this.m_updates.Updates;
						for (int k = 0; k < updates2.Length; k++)
						{
							JamJSONRealmListUpdatePart jamJSONRealmListUpdatePart2 = updates2[k];
							if (jamJSONRealmListUpdatePart2.Update.WowRealmAddress == jamJSONCharacterEntry.VirtualRealmAddress)
							{
								string name = jamJSONRealmListUpdatePart2.Update.Name;
								bool online = jamJSONRealmListUpdatePart2.Update.PopulationState != 0;
								Login.instance.LoginLog(string.Concat(new string[]
								{
									jamJSONCharacterEntry.Name,
									": Adding Character Button. Realm: ",
									name,
									", online: ",
									online.ToString()
								}));
								AllPanels.instance.AddCharacterButton(jamJSONCharacterEntry, this.SubRegion, name, online);
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							Login.instance.LoginLog(jamJSONCharacterEntry.Name + ": Could not find entry for realm address " + jamJSONCharacterEntry.VirtualRealmAddress);
						}
						if (Login.instance.UseCachedCharacter() && @string != null && jamJSONCharacterEntry.PlayerGuid == @string)
						{
							Login.instance.m_selectedCharacterEntry = jamJSONCharacterEntry;
							Login.instance.SendRealmJoinRequest("dummyRealmName", (ulong)jamJSONCharacterEntry.VirtualRealmAddress, this.SubRegion);
							Login.instance.SetLoginState(Login.eLoginState.BN_REALM_JOIN_WAIT);
						}
					}
					Login.instance.CheckRecentCharacters(this.m_characters.CharacterList, this.SubRegion);
				}
				AllPanels.instance.SortCharacterList();
				return;
			}
			throw new Exception("ClientResponse was null in CharacterListCallback for sub region: " + this.SubRegion);
		}
	}

	private class RealmJoinInfo
	{
		private ulong m_realmAddress;

		private string m_ipAddress;

		private byte[] m_joinTicket;

		private byte[] m_joinSecret;

		public ulong RealmAddress
		{
			get
			{
				return this.m_realmAddress;
			}
			set
			{
				this.m_realmAddress = value;
			}
		}

		public string IpAddress
		{
			get
			{
				return this.m_ipAddress;
			}
			set
			{
				this.m_ipAddress = value;
			}
		}

		public byte[] JoinTicket
		{
			get
			{
				return this.m_joinTicket;
			}
			set
			{
				this.m_joinTicket = value;
			}
		}

		public byte[] JoinSecret
		{
			get
			{
				return this.m_joinSecret;
			}
			set
			{
				this.m_joinSecret = value;
			}
		}

		public int Port
		{
			get;
			set;
		}

		public void ClientResponseCallback(RPCContext context)
		{
			ClientResponse clientResponseFromContext = GamesAPI.GetClientResponseFromContext(context);
			if (clientResponseFromContext != null)
			{
				using (List<Attribute>.Enumerator enumerator = clientResponseFromContext.AttributeList.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Attribute current = enumerator.get_Current();
						if (current.Name == "Param_RealmJoinTicket")
						{
							this.JoinTicket = current.Value.BlobValue;
						}
						else if (current.Name == "Param_ServerAddresses")
						{
							string value = Login.DecompressJsonAttribBlob(current.Value.BlobValue);
							JSONRealmListServerIPAddresses jSONRealmListServerIPAddresses = JsonConvert.DeserializeObject<JSONRealmListServerIPAddresses>(value);
							JamJSONRealmListServerIPFamily[] families = jSONRealmListServerIPAddresses.Families;
							for (int i = 0; i < families.Length; i++)
							{
								JamJSONRealmListServerIPFamily jamJSONRealmListServerIPFamily = families[i];
								JamJSONRealmListServerIPAddress[] addresses = jamJSONRealmListServerIPFamily.Addresses;
								for (int j = 0; j < addresses.Length; j++)
								{
									JamJSONRealmListServerIPAddress jamJSONRealmListServerIPAddress = addresses[j];
									if (jamJSONRealmListServerIPAddress.Ip.IndexOf('.') > 0)
									{
										this.IpAddress = jamJSONRealmListServerIPAddress.Ip;
										this.Port = (int)jamJSONRealmListServerIPAddress.Port;
										Login.instance.LoginLog(string.Concat(new object[]
										{
											"RealmJoinInfo: Found ip ",
											this.IpAddress,
											", port ",
											this.Port
										}));
									}
								}
							}
						}
						else if (current.Name == "Param_JoinSecret")
						{
							this.JoinSecret = current.Value.BlobValue;
							Login.instance.SetJoinSecret(current.Value.BlobValue);
						}
					}
				}
				this.RealmJoinConnectToMobileServer();
				return;
			}
			throw new Exception("ClientResponse was null in RealmJoinCallback");
		}

		public void RealmJoinConnectToMobileServer()
		{
			if (this.IpAddress == null || this.IpAddress == string.Empty)
			{
				Login.instance.LoginLog("Couldn't connect to mobile server, ip address was blank.");
				return;
			}
			int num = this.Port;
			if (num == 0)
			{
				num = 6012;
			}
			string bnetAccount = string.Format("BNetAccount-0-{0:X12}", BattleNet.GetMyAccountId().lo);
			string wowAccount = string.Format("WowAccount-0-{0:X12}", Login.instance.m_gameAccount.Low);
			Login.instance.ConnectToMobileServer(this.IpAddress, num, bnetAccount, this.RealmAddress, wowAccount, this.JoinTicket, true);
		}
	}

	private const float m_characterListRefreshWaitTime = 30f;

	private const float m_characterListDisplayTimeout = 10f;

	private const float PING_INTERVAL = 10f;

	private const float PONG_TIMEOUT = 60f;

	public const int m_numRecentChars = 3;

	private const int m_unpauseReconnectTime = 30;

	private const int m_recentCharacterVersion = 1;

	private const float m_bnLoginTimeout = 20f;

	public static Login instance;

	private MobileNetwork m_mobileNetwork;

	private string m_webToken;

	private Login.eLoginState m_loginState = Login.eLoginState.WAIT_FOR_ASSET_BUNDLES;

	private WebAuth m_webAuth;

	private string m_webAuthUrl;

	private string m_mobileServerAddress;

	private int m_mobileServerPort;

	private string m_bnetAccount;

	private ulong m_virtualRealmAddress;

	private string m_wowAccount;

	private byte[] m_realmListTicket;

	private byte[] m_realmJoinTicket;

	private string[] m_subRegions;

	private string m_subRegion;

	private string m_auroraAttributeVersion = "_b9";

	private List<Login.LoginGameAccount> m_loginGameAccounts;

	private float m_characterListStartTime;

	private Login.RealmJoinInfo m_realmJoinInfo;

	private float m_lastPongTime;

	private float m_lastPingTime;

	private bool m_pongReceived;

	private float m_mobileLoginTime;

	private float m_mobileLoginTimeout = 25f;

	public DotNetUrlDownloader m_urlDownloader;

	private int m_expectedServerProtocolVersion = 4;

	private int m_currentServerProtocolVersion = -1;

	private bool m_useCachedLogin;

	private bool m_useCachedRealm;

	private bool m_useCachedCharacter;

	private bool m_useCachedWoWAccount = true;

	public EntityId m_gameAccount;

	private JamJSONCharacterEntry m_selectedCharacterEntry;

	private int m_pauseTimestamp;

	private List<RecentCharacter> m_recentCharacters;

	private byte[] m_clientChallenge;

	private byte[] m_joinSecret;

	private byte[] m_clientSecret;

	private DisconnectReason m_recentDisconnectReason;

	public bool m_clearCharacterListOnReply;

	private float m_bnLoginStartTime;

	private int m_battlenetFailures;

	public static string m_portal = "beta";

	public bool ReturnToRecentCharacter
	{
		get;
		private set;
	}

	private void OnApplicationPause(bool paused)
	{
		this.LoginLog("OnApplicationPause: " + paused.ToString());
		if (paused)
		{
			BattleNet.ApplicationWasPaused();
			this.m_pauseTimestamp = GeneralHelpers.CurrentUnixTime();
		}
		else
		{
			int num = GeneralHelpers.CurrentUnixTime();
			int num2 = num - this.m_pauseTimestamp;
			bool flag = num2 > 30;
			if (flag)
			{
				if (AllPopups.instance != null)
				{
					AllPopups.instance.HideAllPopups();
				}
				if (AdventureMapPanel.instance != null)
				{
					AdventureMapPanel.instance.SelectMissionFromList(0);
				}
				switch (this.m_loginState)
				{
				case Login.eLoginState.IDLE:
				case Login.eLoginState.WAIT_FOR_ASSET_BUNDLES:
				case Login.eLoginState.WEB_AUTH_START:
				case Login.eLoginState.WEB_AUTH_LOADING:
				case Login.eLoginState.WEB_AUTH_IN_PROGRESS:
					break;
				default:
					AllPanels.instance.ShowConnectingPanel();
					break;
				}
			}
			BattleNet.ApplicationWasUnpaused();
			Login.eLoginState loginState = this.m_loginState;
			switch (loginState)
			{
			case Login.eLoginState.WAIT_FOR_ASSET_BUNDLES:
				this.LoginLog("OnApplicationPause: Wait for asset bundles");
				break;
			case Login.eLoginState.WEB_AUTH_START:
			case Login.eLoginState.WEB_AUTH_LOADING:
			case Login.eLoginState.WEB_AUTH_IN_PROGRESS:
				AllPanels.instance.HideConnectingPanel();
				this.LoginLog("OnApplicationPause: Hiding all panels");
				break;
			default:
				switch (loginState)
				{
				case Login.eLoginState.MOBILE_LOGGING_IN:
					this.LoginLog("OnApplicationPause: Reconnecting: mobile login states");
					this.m_battlenetFailures = 0;
					this.m_mobileNetwork.Disconnect();
					this.BnLoginStart(true, true, true, false);
					break;
				case Login.eLoginState.MOBILE_LOGGED_IN:
				case Login.eLoginState.MOBILE_LOGGED_IN_IDLE:
					if (flag)
					{
						this.LoginLog("OnApplicationPause: Reconnecting: mobile idle state");
						this.m_battlenetFailures = 0;
						this.m_mobileNetwork.Disconnect();
						this.BnLoginStart(true, true, true, false);
					}
					else if (this.m_mobileNetwork != null && !this.m_mobileNetwork.IsConnected)
					{
						this.LoginLog("OnApplicationPause: Reconnecting, not connected after short pause time of " + num2);
						this.m_battlenetFailures = 0;
						this.m_mobileNetwork.Disconnect();
						this.BnLoginStart(true, true, true, false);
					}
					else
					{
						this.LoginLog("OnApplicationPause: Still connected. Not reconnecting after short pause time of " + num2);
					}
					break;
				default:
					if (loginState != Login.eLoginState.BN_CHARACTER_LIST_WAIT)
					{
						if (this.m_loginState != Login.eLoginState.IDLE)
						{
							this.LoginLog("OnApplicationPause: Back to Title");
							this.BnLoginStart(true, true, true, false);
						}
					}
					else if (flag)
					{
						this.m_battlenetFailures = 0;
						this.LoginLog("OnApplicationPause: Reconnecting: character list wait state");
						this.BnLoginStart(true, true, false, false);
					}
					else
					{
						this.LoginLog("OnApplicationPause: Not reconnecting after short pause time of " + num2);
					}
					break;
				}
				break;
			}
		}
	}

	private void Awake()
	{
		Login.instance = this;
		this.SetInitialPortal();
	}

	private void Start()
	{
		this.m_urlDownloader = new DotNetUrlDownloader();
		this.SetLoginState(Login.eLoginState.WAIT_FOR_ASSET_BUNDLES);
		this.m_lastPingTime = Time.get_timeSinceLevelLoad();
		this.m_lastPongTime = this.m_lastPingTime;
		this.m_pongReceived = false;
		this.InitRecentCharacters();
		this.LoadRecentCharacters();
		this.ReturnToRecentCharacter = false;
		if (this.IsDevRegionList())
		{
			this.m_mobileLoginTimeout = 120f;
		}
	}

	private void SetLoginState(Login.eLoginState newState)
	{
		this.LoginLog(string.Concat(new string[]
		{
			"SetLoginState(): from ",
			this.m_loginState.ToString(),
			" to ",
			newState.ToString(),
			"."
		}));
		this.m_loginState = newState;
	}

	public Login.eLoginState GetLoginState()
	{
		return this.m_loginState;
	}

	public void PongReceived()
	{
		this.m_pongReceived = true;
	}

	private void UpdatePing()
	{
		if (this.m_loginState != Login.eLoginState.MOBILE_LOGGED_IN_IDLE)
		{
			return;
		}
		if (Time.get_timeSinceLevelLoad() > this.m_lastPingTime + 10f)
		{
			MobilePlayerPing obj = new MobilePlayerPing();
			Login.instance.SendToMobileServer(obj);
			this.m_lastPingTime = Time.get_timeSinceLevelLoad();
		}
		if (this.m_pongReceived)
		{
			this.m_pongReceived = false;
			this.m_lastPongTime = Time.get_timeSinceLevelLoad();
		}
		if (Time.get_timeSinceLevelLoad() > this.m_lastPongTime + 60f)
		{
			Debug.Log(string.Concat(new object[]
			{
				"Server pong timeout at time ",
				Time.get_timeSinceLevelLoad(),
				". Last pong was ",
				Time.get_timeSinceLevelLoad() - this.m_lastPongTime,
				" seconds ago. ",
				this.m_loginState.ToString()
			}));
			this.MobileDisconnect(DisconnectReason.PingTimeout);
			this.m_lastPongTime = Time.get_timeSinceLevelLoad();
		}
	}

	private void Update()
	{
		this.UpdateLoginState();
		this.BnErrorsUpdate();
		this.UpdatePing();
		this.m_urlDownloader.Process();
		MobileNetwork.Process();
	}

	private void UpdateLoginState()
	{
		switch (this.m_loginState)
		{
		case Login.eLoginState.WAIT_FOR_ASSET_BUNDLES:
			this.WaitForAssetBundles();
			break;
		case Login.eLoginState.WEB_AUTH_START:
			this.WebAuthStart();
			break;
		case Login.eLoginState.WEB_AUTH_LOADING:
			this.WebAuthUpdate();
			break;
		case Login.eLoginState.WEB_AUTH_IN_PROGRESS:
			this.WebAuthUpdate();
			break;
		case Login.eLoginState.BN_LOGIN_START:
			this.BnLoginStart(true, true, true, false);
			break;
		case Login.eLoginState.BN_LOGIN_WAIT_FOR_LOGON:
			this.BnLoginWaitForLogon();
			break;
		case Login.eLoginState.BN_LOGIN_PROVIDE_TOKEN:
			this.BnLoginProvideToken();
			break;
		case Login.eLoginState.BN_LOGGING_IN:
		case Login.eLoginState.BN_LOGIN_UNKNOWN:
			this.BnLoginUpdate();
			break;
		case Login.eLoginState.BN_ACCOUNT_NAME_WAIT:
			this.BnAccountNameWait();
			break;
		case Login.eLoginState.BN_TICKET_WAIT:
			this.BnTicketWait();
			break;
		case Login.eLoginState.BN_SUBREGION_LIST_WAIT:
			this.BnSubRegionListWait();
			break;
		case Login.eLoginState.BN_CHARACTER_LIST_WAIT:
			this.BnCharacterListWait();
			break;
		case Login.eLoginState.BN_REALM_JOIN_WAIT:
			this.BnRealmJoinWait();
			break;
		case Login.eLoginState.MOBILE_CONNECT:
			this.MobileConnect();
			break;
		case Login.eLoginState.MOBILE_CONNECTING:
			this.MobileConnecting();
			break;
		case Login.eLoginState.MOBILE_CONNECT_FAILED:
			this.MobileConnectFailed();
			break;
		case Login.eLoginState.MOBILE_DISCONNECTED:
			this.MobileDisconnected();
			break;
		case Login.eLoginState.MOBILE_DISCONNECTED_BY_SERVER:
			this.MobileDisconnectedByServer();
			break;
		case Login.eLoginState.MOBILE_LOGGING_IN:
			this.MobileLoggingIn();
			break;
		case Login.eLoginState.MOBILE_LOGGED_IN:
			this.MobileLoggedIn();
			break;
		}
	}

	private void WaitForAssetBundles()
	{
		if (!AssetBundleManager.instance.IsInitialized())
		{
			return;
		}
		this.GetBnServerString();
		this.LoginLog("Latest version is " + AssetBundleManager.instance.LatestVersion);
		this.LoginLog("Force upgrade is " + AssetBundleManager.instance.ForceUpgrade);
		AllPanels.instance.SetConnectingPanelStatus(StaticDB.GetString("CONNECTING", null));
		AllPanels.instance.ShowConnectingPanelCancelButton(true);
		string @string = SecurePlayerPrefs.GetString("WebToken", Main.uniqueIdentifier);
		if (AssetBundleManager.instance.LatestVersion > 0 && AssetBundleManager.instance.LatestVersion > BuildNum.CodeBuildNum)
		{
			AllPanels.instance.ShowTitlePanel();
			GenericPopup.DisabledAction = (Action)Delegate.Combine(GenericPopup.DisabledAction, new Action(this.UpdateAppPopupDisabledAction));
			AllPopups.instance.ShowGenericPopup(StaticDB.GetString("UPDATE_REQUIRED", null), StaticDB.GetString("UPDATE_REQUIRED_DESCRIPTION", null));
			this.SetLoginState(Login.eLoginState.IDLE);
		}
		else if (@string != null && @string != string.Empty)
		{
			AllPanels.instance.ShowConnectingPanel();
			this.SetLoginState(Login.eLoginState.BN_LOGIN_START);
		}
		else
		{
			AllPanels.instance.ShowTitlePanel();
			this.SetLoginState(Login.eLoginState.IDLE);
		}
	}

	private void UpdateAppPopupDisabledAction()
	{
		GenericPopup.DisabledAction = (Action)Delegate.Remove(GenericPopup.DisabledAction, new Action(this.UpdateAppPopupDisabledAction));
		string text;
		if (Login.instance.GetBnPortal() == "cn")
		{
			text = AssetBundleManager.instance.AppStoreUrl_CN;
		}
		else
		{
			text = AssetBundleManager.instance.AppStoreUrl;
		}
		if (text != null)
		{
			Application.OpenURL(text);
		}
		if (AssetBundleManager.instance.ForceUpgrade)
		{
			Application.Quit();
		}
	}

	public bool IsDevRegionList()
	{
		return false;
	}

	public string GetBnServerString()
	{
		string @string = SecurePlayerPrefs.GetString("Portal", Main.uniqueIdentifier);
		if (@string != null && @string != string.Empty)
		{
			Login.m_portal = @string;
		}
		string text = Login.m_portal + ".actual.battle.net";
		this.LoginLog("BnServerString is " + text);
		return text;
	}

	public string GetBnPortal()
	{
		string @string = SecurePlayerPrefs.GetString("Portal", Main.uniqueIdentifier);
		if (@string != null && @string != string.Empty)
		{
			Login.m_portal = @string;
		}
		this.LoginLog("Portal is " + Login.m_portal);
		return Login.m_portal;
	}

	public void SetPortal(string newValue)
	{
		Login.m_portal = newValue.ToLower();
		SecurePlayerPrefs.SetString("Portal", Login.m_portal, Main.uniqueIdentifier);
		PlayerPrefs.Save();
		Debug.Log("Setting portal to " + Login.m_portal);
	}

	private void SetInitialPortal()
	{
		string @string = SecurePlayerPrefs.GetString("Portal", Main.uniqueIdentifier);
		if (@string != null && @string != string.Empty)
		{
			Login.m_portal = @string;
		}
		else if (!this.IsDevRegionList())
		{
			string locale = Main.instance.GetLocale();
			string text = locale;
			string text2;
			if (text != null)
			{
				if (Login.<>f__switch$map5 == null)
				{
					Dictionary<string, int> dictionary = new Dictionary<string, int>(7);
					dictionary.Add("frFR", 0);
					dictionary.Add("deDE", 0);
					dictionary.Add("ruRU", 0);
					dictionary.Add("itIT", 0);
					dictionary.Add("koKR", 1);
					dictionary.Add("zhTW", 1);
					dictionary.Add("zhCN", 2);
					Login.<>f__switch$map5 = dictionary;
				}
				int num;
				if (Login.<>f__switch$map5.TryGetValue(text, ref num))
				{
					switch (num)
					{
					case 0:
						text2 = "eu";
						goto IL_119;
					case 1:
						text2 = "kr";
						goto IL_119;
					case 2:
						text2 = "cn";
						goto IL_119;
					}
				}
			}
			text2 = "us";
			IL_119:
			Debug.Log("Setting initial portal to " + text2);
			this.SetPortal(text2);
		}
	}

	private void BnErrorsUpdate()
	{
		int errorsCount = BattleNet.GetErrorsCount();
		if (errorsCount > 0)
		{
			BnetErrorInfo[] array = new BnetErrorInfo[errorsCount];
			BattleNet.GetErrors(array);
			BattleNet.ClearErrors();
			BnetErrorInfo[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				BnetErrorInfo bnetErrorInfo = array2[i];
				Debug.Log("Battle.net error: Name: " + bnetErrorInfo.GetName());
				Debug.Log("Battle.net error: Feature: " + bnetErrorInfo.GetFeature());
				Debug.Log("Battle.net error: FeatureEvent: " + bnetErrorInfo.GetFeatureEvent());
				Debug.Log("Battle.net error: Context: " + bnetErrorInfo.GetContext());
				if (bnetErrorInfo.GetError() != bgs.BattleNetErrors.ERROR_RPC_PEER_DISCONNECTED)
				{
					this.BnLoginFailed(null, null);
					return;
				}
				Login.eLoginState loginState = this.m_loginState;
				if (loginState == Login.eLoginState.BN_CHARACTER_LIST_WAIT)
				{
					this.BnLoginStart(true, true, false, false);
				}
			}
		}
		List<string> logEvents = Log.BattleNet.GetLogEvents();
		if (logEvents.get_Count() > 0)
		{
			Log.BattleNet.ClearLogEvents();
		}
	}

	public void LoginLog(string message)
	{
	}

	public void StartNewLogin()
	{
		this.BnLoginStart(false, false, false, false);
	}

	public void StartCachedLogin(bool useCachedRealm, bool useCachedCharacter)
	{
		this.BnLoginStart(true, useCachedRealm, useCachedCharacter, false);
	}

	public void ClearAllCachedTokens()
	{
		SecurePlayerPrefs.DeleteKey("WebToken");
		SecurePlayerPrefs.DeleteKey("CharacterID");
		SecurePlayerPrefs.DeleteKey("CharacterName");
		SecurePlayerPrefs.DeleteKey("GameAccountHigh");
		SecurePlayerPrefs.DeleteKey("GameAccountLow");
		Debug.Log("Clearing cached BN token, game account, and character");
	}

	private void ClearCachedTokens()
	{
		SecurePlayerPrefs.DeleteKey("WebToken");
		SecurePlayerPrefs.DeleteKey("CharacterID");
		Debug.Log("Clearing cached BN token, realm, and character");
	}

	public void ClearRealmAndCharacterTokens()
	{
		SecurePlayerPrefs.DeleteKey("CharacterID");
		Debug.Log("Clearing cached realm and character");
	}

	public bool HaveCachedWebToken()
	{
		string @string = SecurePlayerPrefs.GetString("WebToken", Main.uniqueIdentifier);
		return @string != null && @string != string.Empty;
	}

	public bool UseCachedCharacter()
	{
		return this.m_useCachedCharacter;
	}

	private void BnLoginStart(bool cachedLogin, bool cachedRealm, bool cachedCharacter, bool returnToRecentCharacter = false)
	{
		BattleNet.RequestCloseAurora();
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
		this.m_useCachedLogin = cachedLogin;
		this.m_useCachedRealm = cachedRealm;
		this.m_useCachedCharacter = cachedCharacter;
		this.ReturnToRecentCharacter = returnToRecentCharacter;
		this.m_bnLoginStartTime = Time.get_timeSinceLevelLoad();
		AllPanels.instance.ShowConnectingPanel();
		string bnServerString = this.GetBnServerString();
		int port = 1119;
		ClientInterface ci = new MyClientInterface();
		SslParameters sslParams = new SslParameters();
		this.LoginLog(string.Concat(new string[]
		{
			"BnLoginStart(",
			this.m_useCachedLogin.ToString(),
			",",
			this.m_useCachedRealm.ToString(),
			",",
			this.m_useCachedCharacter.ToString(),
			"): server = ",
			bnServerString
		}));
		bool flag = BattleNet.Init(true, string.Empty, bnServerString, port, sslParams, ci);
		if (flag)
		{
			BattleNet.ProcessAurora();
			this.SetLoginState(Login.eLoginState.BN_LOGIN_WAIT_FOR_LOGON);
		}
		else
		{
			this.LoginLog("BattleNet.Init() failed.");
			this.BnLoginFailed(null, null);
		}
	}

	private void BnLoginWaitForLogon()
	{
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
		if (BattleNet.CheckWebAuth(out this.m_webAuthUrl))
		{
			this.LoginLog("Received WebAuth challenge URL: " + this.m_webAuthUrl);
			this.m_webToken = SecurePlayerPrefs.GetString("WebToken", Main.uniqueIdentifier);
			if (this.m_useCachedLogin && this.m_webToken != null && this.m_webToken != string.Empty)
			{
				this.SetLoginState(Login.eLoginState.BN_LOGIN_PROVIDE_TOKEN);
			}
			else
			{
				this.SetLoginState(Login.eLoginState.WEB_AUTH_START);
			}
		}
		if (Time.get_timeSinceLevelLoad() > this.m_bnLoginStartTime + 20f)
		{
			this.LoginLog("BnLoginWaitForLogon(): timed out.");
			this.BnLoginFailed(null, null);
		}
	}

	private void WebAuthStart()
	{
		AllPanels.instance.HidePanelsForWebAuth();
		if (this.m_webAuth == null)
		{
			GameObject gameObject = GameObject.Find("MainCanvas");
			if (gameObject == null)
			{
				throw new Exception("Canvas game obj was null in WebAuthStart");
			}
			Canvas component = gameObject.GetComponent<Canvas>();
			if (component == null)
			{
				throw new Exception("webCanvas was null in WebAuthStart");
			}
			if (this.m_webAuthUrl == null)
			{
				throw new Exception("m_webAuthUrl was null in WebAuthStart");
			}
			float x = 0f;
			float y = 0f;
			float width = component.get_pixelRect().get_width();
			float height = component.get_pixelRect().get_height();
			this.m_webAuth = new WebAuth(this.m_webAuthUrl, x, y, width, height, "MainScriptObj");
			this.LoginLog("Created WebAuth, url: " + this.m_webAuthUrl);
		}
		if (this.m_webAuth != null)
		{
			this.SetLoginState(Login.eLoginState.WEB_AUTH_IN_PROGRESS);
			string locale = Main.instance.GetLocale();
			string text = locale.Substring(2);
			this.LoginLog("Setting web auth country code to " + text);
			this.m_webAuth.SetCountryCodeCookie(text, ".blizzard.net");
			WebAuth.ClearLoginData();
			this.m_webAuth.Load();
			AllPanels.instance.ShowConnectingPanel();
			this.LoginLog("------------------------------ Loading WebAuth ----------------------------");
			return;
		}
		throw new Exception("m_webAuth was null in WebAuthStart");
	}

	public void ShowWebAuthView()
	{
		if (this.m_webAuth != null && this.m_loginState == Login.eLoginState.WEB_AUTH_IN_PROGRESS)
		{
			this.LoginLog("============================= Showing WebAuth View ========================");
			this.m_webAuth.Show();
			AllPanels.instance.ShowWebAuthPanel();
		}
	}

	public void CancelWebAuth()
	{
		if (this.m_webAuth != null)
		{
			this.m_webAuth.Close();
			this.m_webAuth = null;
		}
	}

	private void WebAuthUpdate()
	{
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
		if (this.m_webAuth == null)
		{
			this.SetLoginState(Login.eLoginState.WEB_AUTH_FAILED);
			this.LoginLog("m_webAuth was null in WebAuthUpdate");
			return;
		}
		WebAuth.Status status = this.m_webAuth.GetStatus();
		if (status != WebAuth.Status.Success)
		{
			if (status == WebAuth.Status.Error)
			{
				this.LoginLog("Web auth status: Error.");
				this.SetLoginState(Login.eLoginState.WEB_AUTH_FAILED);
			}
		}
		else
		{
			this.m_webToken = this.m_webAuth.GetLoginCode();
			this.LoginLog("Received web auth token: " + this.m_webToken);
			this.SetLoginState(Login.eLoginState.BN_LOGIN_PROVIDE_TOKEN);
			this.m_webAuth.Close();
			this.m_webAuth = null;
			AllPanels.instance.ShowConnectingPanel();
		}
	}

	public bool IsWebAuthState()
	{
		switch (this.m_loginState)
		{
		case Login.eLoginState.WEB_AUTH_START:
		case Login.eLoginState.WEB_AUTH_LOADING:
		case Login.eLoginState.WEB_AUTH_IN_PROGRESS:
		case Login.eLoginState.WEB_AUTH_FAILED:
			return true;
		default:
			return false;
		}
	}

	private void BnLoginProvideToken()
	{
		BattleNet.ProcessAurora();
		BattleNet.ProvideWebAuthToken(this.m_webToken);
		this.LoginLog("BnLoginProvideToken(): Provided web auth token " + this.m_webToken);
		this.SetLoginState(Login.eLoginState.BN_LOGGING_IN);
	}

	private void BnLoginUpdate()
	{
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
		if (BattleNet.CheckWebAuth(out this.m_webAuthUrl))
		{
			this.LoginLog("CheckWebAuth was true in BnLoginUpdate, starting WebAuth.");
			this.SetLoginState(Login.eLoginState.WEB_AUTH_START);
		}
		switch (BattleNet.BattleNetStatus())
		{
		case 0:
			if (this.m_loginState != Login.eLoginState.BN_LOGIN_UNKNOWN)
			{
				this.SetLoginState(Login.eLoginState.BN_LOGIN_UNKNOWN);
			}
			break;
		case 1:
			if (this.m_loginState != Login.eLoginState.BN_LOGGING_IN)
			{
				this.SetLoginState(Login.eLoginState.BN_LOGGING_IN);
			}
			break;
		case 2:
			this.LoginLog("BnLoginUpdate(): timed out.");
			this.BnLoginFailed(null, null);
			break;
		case 3:
			this.LoginLog("BnLoginUpdate(): login failed.");
			this.BnLoginFailed(null, null);
			break;
		case 4:
			this.LoginLog("-------------------BnLoginUpdate(): Success! Logged in.--------------------------");
			this.BnLoginSucceeded();
			break;
		}
	}

	private void BnLoginSucceeded()
	{
		this.m_battlenetFailures = 0;
		SecurePlayerPrefs.SetString("WebToken", this.m_webToken, Main.uniqueIdentifier);
		PlayerPrefs.Save();
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
		if (this.m_loginGameAccounts == null)
		{
			this.m_loginGameAccounts = new List<Login.LoginGameAccount>();
		}
		this.m_loginGameAccounts.Clear();
		this.RequestGameAccountNames();
	}

	private void RegisterPushManager(string token, string locale)
	{
		BLPushManagerBuilder bLPushManagerBuilder = ScriptableObject.CreateInstance<BLPushManagerBuilder>();
		if (Login.m_portal.ToLower() == "wow-dev")
		{
			bLPushManagerBuilder.isDebug = true;
			bLPushManagerBuilder.applicationName = "test.wowcompanion";
		}
		else
		{
			bLPushManagerBuilder.isDebug = false;
			bLPushManagerBuilder.applicationName = "wowcompanion";
		}
		bLPushManagerBuilder.shouldRegisterwithBPNS = true;
		bLPushManagerBuilder.region = "US";
		bLPushManagerBuilder.locale = locale;
		bLPushManagerBuilder.authToken = token;
		bLPushManagerBuilder.authRegion = "US";
		bLPushManagerBuilder.appAccountID = string.Empty;
		bLPushManagerBuilder.senderId = "952133414280";
		bLPushManagerBuilder.didReceiveRegistrationTokenDelegate = new DidReceiveRegistrationTokenDelegate(this.DidReceiveRegistrationTokenHandler);
		bLPushManagerBuilder.didReceiveDeeplinkURLDelegate = new DidReceiveDeeplinkURLDelegate(this.DidReceiveDeeplinkURLDelegateHandler);
		BLPushManager.instance.InitWithBuilder(bLPushManagerBuilder);
		BLPushManager.instance.RegisterForPushNotifications();
	}

	public void DidReceiveRegistrationTokenHandler(string deviceToken)
	{
		Debug.Log("DidReceiveRegistrationTokenHandler: device token " + deviceToken);
	}

	public void DidReceiveDeeplinkURLDelegateHandler(string url)
	{
		Debug.Log("DidReceiveDeeplinkURLDelegateHandler: url " + url);
	}

	public void AddGameAccountButton(EntityId gameAccount, string name, bool isBanned, bool isSuspended)
	{
		if (this.m_loginState != Login.eLoginState.BN_ACCOUNT_NAME_WAIT)
		{
			this.LoginLog("AddGameAccountButton(): Ignored because in state " + this.m_loginState);
			return;
		}
		RealmListView component = AllPanels.instance.realmListPanel.m_realmListView.GetComponent<RealmListView>();
		if (this.m_loginGameAccounts.get_Count() == 0)
		{
			component.ClearBnRealmList();
		}
		Login.LoginGameAccount loginGameAccount = new Login.LoginGameAccount();
		loginGameAccount.gameAccount = gameAccount;
		loginGameAccount.name = name;
		loginGameAccount.isBanned = isBanned;
		loginGameAccount.isSuspended = isSuspended;
		this.m_loginGameAccounts.Add(loginGameAccount);
		List<EntityId> gameAccountList = BattleNet.GetGameAccountList();
		if (gameAccountList.get_Count() > 1)
		{
			string @string = SecurePlayerPrefs.GetString("GameAccountHigh", Main.uniqueIdentifier);
			string string2 = SecurePlayerPrefs.GetString("GameAccountLow", Main.uniqueIdentifier);
			ulong num = 0uL;
			ulong num2 = 0uL;
			if (@string != null && @string != string.Empty)
			{
				num = Convert.ToUInt64(@string);
			}
			if (string2 != null && string2 != string.Empty)
			{
				num2 = Convert.ToUInt64(string2);
			}
			if (this.m_useCachedWoWAccount)
			{
				if (num == gameAccount.High && num2 == gameAccount.Low)
				{
					if (this.m_useCachedLogin && !isBanned && !isSuspended)
					{
						if (this.m_gameAccount == null)
						{
							this.m_gameAccount = new EntityId();
						}
						this.m_gameAccount.High = num;
						this.m_gameAccount.Low = num2;
						this.SetLoginState(Login.eLoginState.BN_TICKET_WAIT);
						this.SendRealmListTicketRequest();
					}
				}
				else if (this.m_loginGameAccounts.get_Count() >= gameAccountList.get_Count() && !AllPanels.instance.IsShowingRealmListPanel())
				{
					this.LoginLog(string.Concat(new object[]
					{
						"Called ShowingRealmListPanel() because all accounts received. ",
						this.m_loginGameAccounts.get_Count(),
						" > ",
						gameAccountList.get_Count()
					}));
					AllPanels.instance.ShowRealmListPanel();
				}
			}
			else if (!AllPanels.instance.IsShowingRealmListPanel())
			{
				this.LoginLog("Called ShowingRealmListPanel() because m_useCachedWoWAccount is false.");
				AllPanels.instance.ShowRealmListPanel();
			}
		}
		else
		{
			if (isBanned)
			{
				this.BnLoginFailed(StaticDB.GetString("SUSPENDED", null), StaticDB.GetString("SUSPENDED_LONG", null));
				return;
			}
			if (isSuspended)
			{
				this.BnLoginFailed(StaticDB.GetString("SUSPENDED", null), StaticDB.GetString("SUSPENDED_LONG", null));
				return;
			}
			this.m_gameAccount = gameAccountList.get_Item(0);
			this.SetLoginState(Login.eLoginState.BN_TICKET_WAIT);
			this.SendRealmListTicketRequest();
		}
		component.AddGameAccountButton(gameAccount, name, isBanned, isSuspended);
	}

	public void SelectGameAccount(EntityId gameAccount)
	{
		this.m_useCachedWoWAccount = true;
		Login.LoginGameAccount loginGameAccount = null;
		using (List<Login.LoginGameAccount>.Enumerator enumerator = this.m_loginGameAccounts.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				Login.LoginGameAccount current = enumerator.get_Current();
				if (current.gameAccount == gameAccount)
				{
					loginGameAccount = current;
				}
			}
		}
		if (loginGameAccount == null)
		{
			this.LoginLog("SelectGameAccount: Could not find account " + gameAccount.ToString());
			this.BnLoginFailed(null, null);
			return;
		}
		if (loginGameAccount.isBanned)
		{
			List<EntityId> gameAccountList = BattleNet.GetGameAccountList();
			if (gameAccountList.get_Count() > 1)
			{
				AllPopups.instance.ShowGenericPopup(StaticDB.GetString("BANNED", null), StaticDB.GetString("BANNED_LONG", null));
			}
			else
			{
				this.BnLoginFailed(StaticDB.GetString("BANNED", null), StaticDB.GetString("BANNED_LONG", null));
			}
			return;
		}
		if (loginGameAccount.isSuspended)
		{
			List<EntityId> gameAccountList2 = BattleNet.GetGameAccountList();
			if (gameAccountList2.get_Count() > 1)
			{
				AllPopups.instance.ShowGenericPopup(StaticDB.GetString("SUSPENDED", null), StaticDB.GetString("SUSPENDED_LONG", null));
			}
			else
			{
				this.BnLoginFailed(StaticDB.GetString("SUSPENDED", null), StaticDB.GetString("SUSPENDED_LONG", null));
			}
			return;
		}
		SecurePlayerPrefs.SetString("GameAccountHigh", gameAccount.High.ToString(), Main.uniqueIdentifier);
		SecurePlayerPrefs.SetString("GameAccountLow", gameAccount.Low.ToString(), Main.uniqueIdentifier);
		PlayerPrefs.Save();
		this.m_gameAccount = gameAccount;
		this.SetLoginState(Login.eLoginState.BN_TICKET_WAIT);
		this.SendRealmListTicketRequest();
		RealmListView component = AllPanels.instance.realmListPanel.m_realmListView.GetComponent<RealmListView>();
		component.ClearBnRealmList();
		component.SetRealmListTitle();
	}

	private void RequestGameAccountNames()
	{
		this.SetLoginState(Login.eLoginState.BN_ACCOUNT_NAME_WAIT);
		List<EntityId> gameAccountList = BattleNet.GetGameAccountList();
		using (List<EntityId>.Enumerator enumerator = gameAccountList.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				EntityId current = enumerator.get_Current();
				Login.GameAccountStateCallback gameAccountStateCallback = new Login.GameAccountStateCallback();
				gameAccountStateCallback.EntityID = current;
				GetGameAccountStateRequest getGameAccountStateRequest = new GetGameAccountStateRequest();
				getGameAccountStateRequest.GameAccountId = current;
				getGameAccountStateRequest.Options = new GameAccountFieldOptions();
				getGameAccountStateRequest.Options.SetFieldGameLevelInfo(true);
				getGameAccountStateRequest.Options.SetFieldGameStatus(true);
				BattleNet.GetGameAccountState(getGameAccountStateRequest, new RPCContextDelegate(gameAccountStateCallback.Callback));
			}
		}
	}

	private void BnAccountNameWait()
	{
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
		RealmListView component = AllPanels.instance.realmListPanel.m_realmListView.GetComponent<RealmListView>();
		component.SetGameAccountTitle();
	}

	private void SendRealmListTicketRequest()
	{
		this.LoginLog("Sending realm list ticket ClientRequest...");
		ClientRequest clientRequest = new ClientRequest();
		Attribute attribute = new Attribute();
		attribute.SetName("Command_RealmListTicketRequest_v1" + this.m_auroraAttributeVersion);
		Variant variant = new Variant();
		variant.SetIntValue(0L);
		attribute.SetValue(variant);
		clientRequest.AddAttribute(attribute);
		Attribute attribute2 = new Attribute();
		attribute2.SetName("Param_Identity");
		JSONRealmListTicketIdentity jSONRealmListTicketIdentity = new JSONRealmListTicketIdentity();
		byte b = (byte)((this.m_gameAccount.High & 1095216660480uL) >> 32);
		jSONRealmListTicketIdentity.GameAccountRegion = b;
		jSONRealmListTicketIdentity.GameAccountID = this.m_gameAccount.Low;
		this.LoginLog("Region is " + b);
		this.LoginLog("GameAccountRegion: " + jSONRealmListTicketIdentity.GameAccountRegion);
		this.LoginLog("GameAccountID: " + jSONRealmListTicketIdentity.GameAccountID);
		this.LoginLog(string.Format("BNetAccount-0-{0:X12}", BattleNet.GetMyAccountId().lo));
		this.LoginLog(string.Format("WowAccount-0-{0:X12}", this.m_gameAccount.Low));
		string text = jSONRealmListTicketIdentity.ToString().Substring(jSONRealmListTicketIdentity.ToString().LastIndexOf(".") + 1) + ":";
		text += JsonConvert.SerializeObject(jSONRealmListTicketIdentity, Formatting.None);
		text += '\0';
		Variant variant2 = new Variant();
		variant2.SetBlobValue(Encoding.get_UTF8().GetBytes(text));
		this.LoginLog("Identity: <" + Encoding.get_UTF8().GetString(variant2.BlobValue) + ">");
		attribute2.SetValue(variant2);
		clientRequest.AddAttribute(attribute2);
		Attribute attribute3 = new Attribute();
		attribute3.SetName("Param_ClientInfo");
		FourCC fourCC = new FourCC();
		fourCC.SetString("WoW");
		FourCC fourCC2 = new FourCC();
		fourCC2.SetString(BattleNet.Client().GetPlatformName());
		FourCC fourCC3 = new FourCC();
		fourCC3.SetString(Main.instance.GetLocale());
		JSONRealmListTicketClientInformation jSONRealmListTicketClientInformation = new JSONRealmListTicketClientInformation();
		jSONRealmListTicketClientInformation.Info = new JamJSONRealmListTicketClientInformation();
		jSONRealmListTicketClientInformation.Info.Type = fourCC.GetValue();
		jSONRealmListTicketClientInformation.Info.Platform = fourCC2.GetValue();
		jSONRealmListTicketClientInformation.Info.BuildVariant = "darwin-arm-clang-debug";
		jSONRealmListTicketClientInformation.Info.TextLocale = fourCC3.GetValue();
		jSONRealmListTicketClientInformation.Info.AudioLocale = fourCC3.GetValue();
		jSONRealmListTicketClientInformation.Info.Version = new JamJSONGameVersion();
		jSONRealmListTicketClientInformation.Info.Version.VersionMajor = 7u;
		jSONRealmListTicketClientInformation.Info.Version.VersionMinor = 0u;
		jSONRealmListTicketClientInformation.Info.Version.VersionRevision = 3u;
		jSONRealmListTicketClientInformation.Info.Version.VersionBuild = 21980u;
		jSONRealmListTicketClientInformation.Info.VersionDataBuild = 21980u;
		jSONRealmListTicketClientInformation.Info.CurrentTime = (int)BattleNet.Get().CurrentUTCTime();
		jSONRealmListTicketClientInformation.Info.TimeZone = "Etc/UTC";
		this.m_clientSecret = WowAuthCrypto.GenerateSecret();
		jSONRealmListTicketClientInformation.Info.Secret = this.m_clientSecret;
		this.LoginLog("clientInfoJSON.Info.Type is " + jSONRealmListTicketClientInformation.Info.Type);
		this.LoginLog("clientInfoJSON.Info.Textlocale is " + jSONRealmListTicketClientInformation.Info.TextLocale);
		this.LoginLog("clientInfoJSON.Info.Version.VersionBuild is " + jSONRealmListTicketClientInformation.Info.Version.VersionBuild);
		string text2 = jSONRealmListTicketClientInformation.ToString().Substring(jSONRealmListTicketClientInformation.ToString().LastIndexOf(".") + 1) + ":";
		text2 += JsonConvert.SerializeObject(jSONRealmListTicketClientInformation, Formatting.None, MessageFactory.SerializerSettings);
		text2 += '\0';
		Variant variant3 = new Variant();
		variant3.SetBlobValue(Encoding.get_UTF8().GetBytes(text2));
		this.LoginLog("info: <" + Encoding.get_UTF8().GetString(variant3.BlobValue) + ">");
		attribute3.SetValue(variant3);
		clientRequest.AddAttribute(attribute3);
		BattleNet.Get().SendClientRequest(clientRequest, null);
		this.LoginLog("-------------------------Sent realm list TICKET request--------------------------");
	}

	private void BnTicketWait()
	{
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
		bool flag = false;
		GamesAPI.UtilResponse utilResponse = BattleNet.NextUtilPacket();
		if (utilResponse != null)
		{
			this.LoginLog("BnTicketWait(): Received ClientResponse: <" + utilResponse.GetType().ToString() + ">");
			using (List<Attribute>.Enumerator enumerator = utilResponse.m_response.AttributeList.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Attribute current = enumerator.get_Current();
					this.LoginLog("Attrib: <" + current.Name + ">");
					this.m_realmListTicket = current.Value.BlobValue;
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.SetLoginState(Login.eLoginState.BN_SUBREGION_LIST_WAIT);
			this.SendSubRegionListRequest();
		}
	}

	private void SendSubRegionListRequest()
	{
		BattleNet.GetAllValuesForAttribute("Command_CharacterListRequest_v1" + this.m_auroraAttributeVersion, 1);
	}

	private void BnSubRegionListWait()
	{
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
		GamesAPI.GetAllValuesForAttributeResult getAllValuesForAttributeResult = BattleNet.NextGetAllValuesForAttributeResult();
		if (getAllValuesForAttributeResult != null)
		{
			AllPanels.instance.ClearCharacterList();
			this.m_selectedCharacterEntry = null;
			int num = 0;
			this.m_subRegions = new string[getAllValuesForAttributeResult.m_response.AttributeValue.get_Count()];
			using (List<Variant>.Enumerator enumerator = getAllValuesForAttributeResult.m_response.AttributeValue.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Variant current = enumerator.get_Current();
					this.m_subRegions[num++] = current.StringValue;
				}
			}
			string[] subRegions = this.m_subRegions;
			for (int i = 0; i < subRegions.Length; i++)
			{
				string text = subRegions[i];
				this.LoginLog("Received sub region " + text);
				this.SendCharacterListRequest(text);
			}
			this.SetLoginState(Login.eLoginState.BN_CHARACTER_LIST_WAIT);
			AllPanels.instance.ShowConnectingPanel();
			RealmListView component = AllPanels.instance.realmListPanel.m_realmListView.GetComponent<RealmListView>();
			component.ClearBnRealmList();
			this.m_characterListStartTime = Time.get_timeSinceLevelLoad();
		}
	}

	private void SendCharacterListRequest(string subRegion)
	{
		this.LoginLog("Sending realm list ClientRequest for subregion " + subRegion);
		ClientRequest clientRequest = new ClientRequest();
		Attribute attribute = new Attribute();
		attribute.SetName("Command_CharacterListRequest_v1" + this.m_auroraAttributeVersion);
		Variant variant = new Variant();
		variant.SetStringValue(subRegion);
		attribute.SetValue(variant);
		clientRequest.AddAttribute(attribute);
		Attribute attribute2 = new Attribute();
		attribute2.SetName("Param_RealmListTicket");
		Variant variant2 = new Variant();
		variant2.SetBlobValue(this.m_realmListTicket);
		attribute2.SetValue(variant2);
		clientRequest.AddAttribute(attribute2);
		Login.CharacterListCallback characterListCallback = new Login.CharacterListCallback();
		characterListCallback.SubRegion = subRegion;
		BattleNet.Get().SendClientRequest(clientRequest, new RPCContextDelegate(characterListCallback.Callback));
		this.LoginLog("---------------------------Sent Character List request for subregion " + subRegion + " -------------------------------");
	}

	private void BnCharacterListWait()
	{
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
		if (AllPanels.instance.IsShowingCharacterListPanel())
		{
			if (Time.get_timeSinceLevelLoad() > this.m_characterListStartTime + 30f)
			{
				this.m_characterListStartTime = Time.get_timeSinceLevelLoad();
				this.m_clearCharacterListOnReply = true;
				string[] subRegions = this.m_subRegions;
				for (int i = 0; i < subRegions.Length; i++)
				{
					string subRegion = subRegions[i];
					this.SendCharacterListRequest(subRegion);
				}
				this.LoginLog("Refreshing character list.");
			}
		}
		else if (!Login.instance.UseCachedCharacter() || Time.get_timeSinceLevelLoad() > this.m_characterListStartTime + 10f)
		{
			this.LoginLog("Displayed CharacterList panel after timeout.");
			AllPanels.instance.ShowCharacterListPanel();
		}
	}

	public void SelectCharacterNew(JamJSONCharacterEntry characterEntry, string subRegion)
	{
		this.m_selectedCharacterEntry = characterEntry;
		this.SendRealmJoinRequest("dummyRealmName", (ulong)characterEntry.VirtualRealmAddress, subRegion);
		Login.instance.SetLoginState(Login.eLoginState.BN_REALM_JOIN_WAIT);
		AllPanels.instance.ShowConnectingPanel();
		SecurePlayerPrefs.SetString("CharacterID", characterEntry.PlayerGuid, Main.uniqueIdentifier);
		SecurePlayerPrefs.SetString("CharacterName", characterEntry.Name, Main.uniqueIdentifier);
		PlayerPrefs.Save();
	}

	public void SelectRecentCharacter(RecentCharacter recentChar, string subRegion)
	{
		SecurePlayerPrefs.SetString("WebToken", recentChar.WebToken, Main.uniqueIdentifier);
		SecurePlayerPrefs.SetString("GameAccountHigh", recentChar.GameAccount.High.ToString(), Main.uniqueIdentifier);
		SecurePlayerPrefs.SetString("GameAccountLow", recentChar.GameAccount.Low.ToString(), Main.uniqueIdentifier);
		SecurePlayerPrefs.SetString("CharacterID", recentChar.Entry.PlayerGuid, Main.uniqueIdentifier);
		SecurePlayerPrefs.SetString("CharacterName", recentChar.Entry.Name, Main.uniqueIdentifier);
		PlayerPrefs.Save();
		if (this.m_mobileNetwork != null)
		{
			this.m_mobileNetwork.Disconnect();
		}
		this.BnLoginStart(true, true, true, false);
	}

	public void SelectCharacter(string characterGUID, string characterName)
	{
	}

	public void SendRealmJoinRequest(string realmName, ulong realmAddress, string subRegion)
	{
		this.LoginLog(string.Concat(new object[]
		{
			"------------------ Joining realm at address: ",
			realmAddress,
			", sub region ",
			subRegion,
			" --------------------------"
		}));
		this.m_subRegion = subRegion;
		ClientRequest clientRequest = new ClientRequest();
		Attribute attribute = new Attribute();
		attribute.SetName("Command_RealmJoinRequest_v1" + this.m_auroraAttributeVersion);
		Variant variant = new Variant();
		variant.SetStringValue(subRegion);
		attribute.SetValue(variant);
		clientRequest.AddAttribute(attribute);
		Attribute attribute2 = new Attribute();
		attribute2.SetName("Param_RealmAddress");
		Variant variant2 = new Variant();
		variant2.SetUintValue(realmAddress);
		attribute2.SetValue(variant2);
		clientRequest.AddAttribute(attribute2);
		Attribute attribute3 = new Attribute();
		attribute3.SetName("Param_RealmListTicket");
		Variant variant3 = new Variant();
		variant3.SetBlobValue(this.m_realmListTicket);
		attribute3.SetValue(variant3);
		clientRequest.AddAttribute(attribute3);
		Attribute attribute4 = new Attribute();
		attribute4.SetName("Param_BnetSessionKey");
		Variant variant4 = new Variant();
		variant4.SetBlobValue(BattleNet.GetSessionKey());
		attribute4.SetValue(variant4);
		clientRequest.AddAttribute(attribute4);
		this.m_realmJoinInfo = new Login.RealmJoinInfo();
		this.m_realmJoinInfo.RealmAddress = realmAddress;
		BattleNet.Get().SendClientRequest(clientRequest, new RPCContextDelegate(this.m_realmJoinInfo.ClientResponseCallback));
		PlayerPrefs.Save();
		this.LoginLog("----------------------------Sent realm join info request------------------------------");
	}

	private void BnRealmJoinWait()
	{
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
	}

	public static string DecompressJsonAttribBlob(byte[] data)
	{
		MemoryStream memoryStream = new MemoryStream(data);
		byte[] array = new byte[4];
		memoryStream.Read(array, 0, 4);
		int num = BitConverter.ToInt32(array, 0);
		Inflater inflater = new Inflater(false);
		InflaterInputStream inflaterInputStream = new InflaterInputStream(memoryStream, inflater);
		byte[] array2 = new byte[num];
		int num2 = 0;
		int num3 = array2.Length;
		try
		{
			while (true)
			{
				int num4 = inflaterInputStream.Read(array2, num2, num3);
				if (num4 <= 0)
				{
					break;
				}
				num2 += num4;
				num3 -= num4;
			}
		}
		catch (Exception)
		{
			string result = null;
			return result;
		}
		if (num2 != num)
		{
			return null;
		}
		string @string = Encoding.get_UTF8().GetString(array2);
		string text = @string.Substring(@string.IndexOf(':') + 1);
		return text.Substring(0, text.get_Length() - 1);
	}

	private void BnLoginFailed(string popupTitle = null, string popupDescription = null)
	{
		AllPopups.instance.HideAllPopups();
		BattleNet.Get().RequestCloseAurora();
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
		AllPanels.instance.ShowTitlePanel();
		if (popupTitle != null && popupDescription != null)
		{
			AllPopups.instance.ShowGenericPopup(popupTitle, popupDescription);
		}
		else if (popupTitle != null)
		{
			AllPopups.instance.ShowGenericPopupFull(popupTitle);
		}
		else if (this.m_battlenetFailures == 0)
		{
			GenericPopup.DisabledAction = (Action)Delegate.Combine(GenericPopup.DisabledAction, new Action(this.ReconnectPopupDisabledAction));
			if (Main.instance.GetLocale() == "enUS")
			{
				AllPopups.instance.ShowGenericPopup("Battle.net Error", "Unable to contact Battle.net, tap anywhere to retry.");
			}
			else
			{
				AllPopups.instance.ShowGenericPopup(StaticDB.GetString("NETWORK_ERROR", null), StaticDB.GetString("CANT_CONNECT", null));
			}
		}
		else
		{
			AllPopups.instance.ShowGenericPopup(StaticDB.GetString("NETWORK_ERROR", null), StaticDB.GetString("CANT_CONNECT", null));
		}
		this.m_battlenetFailures++;
		this.SetLoginState(Login.eLoginState.IDLE);
		Debug.Log("=================== BN Login Failed. " + this.m_battlenetFailures + " ===================");
	}

	private void ReconnectPopupDisabledAction()
	{
		GenericPopup.DisabledAction = (Action)Delegate.Remove(GenericPopup.DisabledAction, new Action(this.ReconnectPopupDisabledAction));
		if (this.m_mobileNetwork != null)
		{
			this.m_mobileNetwork.Disconnect();
		}
		this.BnLoginStart(true, true, true, false);
	}

	public void BnQuit()
	{
		BattleNet.AppQuit();
	}

	public void ConnectToMobileServer(string mobileServerAddress, int mobileServerPort, string bnetAccount, ulong virtualRealmAddress, string wowAccount, byte[] realmJoinTicket, bool showConnectingPanel = true)
	{
		this.m_mobileServerAddress = mobileServerAddress;
		this.m_mobileServerPort = mobileServerPort;
		this.m_bnetAccount = bnetAccount;
		this.m_virtualRealmAddress = virtualRealmAddress;
		this.m_wowAccount = wowAccount;
		this.m_realmJoinTicket = realmJoinTicket;
		if (showConnectingPanel)
		{
			AllPanels.instance.ShowConnectingPanel();
		}
		this.SetLoginState(Login.eLoginState.MOBILE_CONNECT);
		BattleNet.Get().RequestCloseAurora();
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
	}

	public void ReconnectToMobileServerCharacterSelect()
	{
		if (this.m_mobileNetwork != null)
		{
			this.m_mobileNetwork.Disconnect();
		}
		this.BnLoginStart(true, true, false, true);
	}

	public void ReconnectToMobileServerCharacter()
	{
		if (this.m_mobileNetwork != null)
		{
			this.m_mobileNetwork.Disconnect();
		}
		this.m_useCachedCharacter = true;
		this.ConnectToMobileServer(this.m_mobileServerAddress, this.m_mobileServerPort, this.m_bnetAccount, this.m_virtualRealmAddress, this.m_wowAccount, this.m_realmJoinTicket, true);
	}

	private void MobileConnect()
	{
		if (this.m_mobileNetwork == null)
		{
			this.m_mobileNetwork = new MobileNetwork();
			this.m_mobileNetwork.ConnectionStateChanged += new EventHandler<EventArgs>(this.ClientMobileConnectStateChangeCB);
			this.m_mobileNetwork.ServerDisconnectedEventHandler += new EventHandler<EventArgs>(this.MobileNetworkServerDisconnectedCB);
			this.m_mobileNetwork.ServerConnectionLostEventHandler += new EventHandler<EventArgs>(this.MobileNetworkServerConnectionLostCB);
			this.m_mobileNetwork.MessageReceivedEventHandler += new EventHandler<MobileNetwork.MobileNetworkEventArgs>(Main.instance.OnMessageReceivedCB);
			this.m_mobileNetwork.UnknownMessageReceivedEventHandler += new EventHandler<EventArgs>(Main.instance.OnUnknownMessageReceivedCB);
		}
		if (this.m_mobileNetwork == null)
		{
			this.SetLoginState(Login.eLoginState.MOBILE_CONNECT_FAILED);
			return;
		}
		this.LoginLog("Connecting to: " + this.m_mobileServerAddress);
		bool flag = this.m_mobileNetwork.ConnectAsync(this.m_mobileServerAddress, this.m_mobileServerPort);
		if (flag)
		{
			this.m_lastPingTime = Time.get_timeSinceLevelLoad();
			this.m_lastPongTime = this.m_lastPingTime;
			this.m_pongReceived = false;
			this.m_mobileLoginTime = Time.get_timeSinceLevelLoad();
			this.SetLoginState(Login.eLoginState.MOBILE_CONNECTING);
		}
		else
		{
			Debug.Log(string.Concat(new object[]
			{
				"MobileConnect(): Connect call failed. Address:",
				this.m_mobileServerAddress,
				", port ",
				this.m_mobileServerPort,
				" ."
			}));
			this.SetLoginState(Login.eLoginState.MOBILE_CONNECT_FAILED);
		}
	}

	private void MobileConnecting()
	{
		if (this.m_mobileNetwork == null)
		{
			Debug.Log("MobileConnecting(): Network object was null");
			this.SetLoginState(Login.eLoginState.MOBILE_CONNECT_FAILED);
			return;
		}
		if (Time.get_timeSinceLevelLoad() > this.m_mobileLoginTime + this.m_mobileLoginTimeout)
		{
			Debug.Log("MobileConnecting(): timeout exceeded while connecting");
			this.SetLoginState(Login.eLoginState.MOBILE_CONNECT_FAILED);
			return;
		}
		if (this.m_mobileNetwork.IsConnected)
		{
			this.MobileConnected();
		}
	}

	private void ClientMobileConnectStateChangeCB(object sender, EventArgs e)
	{
		this.LoginLog("ClientMobileConnectStateChangeCB() called");
		if (!this.m_mobileNetwork.IsConnected)
		{
			if (this.m_loginState != Login.eLoginState.MOBILE_DISCONNECTED)
			{
				this.LoginLog("ClientMobileConnectStateChangeCB() setting state to disconnected.");
				this.SetLoginState(Login.eLoginState.MOBILE_DISCONNECTED);
			}
		}
	}

	private void MobileNetworkServerDisconnectedCB(object sender, EventArgs e)
	{
		this.SetLoginState(Login.eLoginState.MOBILE_DISCONNECTED_BY_SERVER);
	}

	private void MobileNetworkServerConnectionLostCB(object sender, EventArgs e)
	{
		this.m_recentDisconnectReason = DisconnectReason.ConnectionLost;
	}

	private void MobileConnected()
	{
		this.LoginLog("============= Mobile Connected ==================");
		this.SetLoginState(Login.eLoginState.MOBILE_LOGGING_IN);
	}

	private void MobileConnectFailed()
	{
		AllPanels.instance.ShowTitlePanel();
		this.SetLoginState(Login.eLoginState.IDLE);
	}

	public void SetJoinSecret(byte[] secret)
	{
		this.m_joinSecret = secret;
	}

	public void MobileAuthChallengeReceived(byte[] serverChallenge)
	{
		this.m_mobileLoginTime = Time.get_timeSinceLevelLoad();
		MobileServerConnect mobileServerConnect = new MobileServerConnect();
		mobileServerConnect.JoinTicket = this.m_realmJoinTicket;
		mobileServerConnect.CharacterID = this.m_selectedCharacterEntry.PlayerGuid;
		mobileServerConnect.RealmAddress = this.m_selectedCharacterEntry.VirtualRealmAddress;
		mobileServerConnect.Build = (ushort)BuildNum.CodeBuildNum;
		this.m_clientChallenge = WowAuthCrypto.GenerateChallenge();
		mobileServerConnect.ClientChallenge = this.m_clientChallenge;
		mobileServerConnect.Proof = WowAuthCrypto.ProveRealmJoinChallenge(this.m_clientSecret, this.m_joinSecret, this.m_clientChallenge, serverChallenge);
		FourCC fourCC = new FourCC();
		fourCC.SetString("WoW");
		mobileServerConnect.BuildType = fourCC.GetValue();
		this.m_mobileNetwork.SendMessage(mobileServerConnect);
		this.SetLoginState(Login.eLoginState.MOBILE_LOGGING_IN);
	}

	private void MobileLoggingIn()
	{
		if (Time.get_timeSinceLevelLoad() > this.m_mobileLoginTime + this.m_mobileLoginTimeout)
		{
			Debug.Log("Mobile login attempt timed out.");
			this.MobileDisconnect(DisconnectReason.TimeoutContactingServer);
		}
	}

	public void MobileConnectResult(MobileClientConnectResult msg)
	{
		this.LoginLog("Connect Result: " + msg.Result.ToString());
		if (this.m_loginState != Login.eLoginState.MOBILE_LOGGING_IN)
		{
			Debug.Log("MobileLoginResult: Ignoring login result while in state " + this.m_loginState.ToString());
			return;
		}
		if (msg.Result == MOBILE_CONNECT_RESULT.MOBILE_CONNECT_RESULT_SUCCESS)
		{
			this.m_currentServerProtocolVersion = msg.Version;
			if (msg.Version == this.m_expectedServerProtocolVersion)
			{
				this.UpdateRecentCharacters();
				this.LoginLog("MobileClientLoginResult success");
				this.SetLoginState(Login.eLoginState.MOBILE_LOGGED_IN);
			}
			else if (msg.Version > this.m_expectedServerProtocolVersion)
			{
				this.LoginLog("App version is too old for server.");
				this.MobileDisconnect(DisconnectReason.AppVersionOld);
			}
			else
			{
				this.LoginLog("App version is too new for server.");
				this.MobileDisconnect(DisconnectReason.AppVersionNew);
			}
		}
		else
		{
			this.m_currentServerProtocolVersion = -1;
			Debug.Log("MobileClientLoginResult failure");
			DisconnectReason reason = DisconnectReason.Generic;
			switch (msg.Result)
			{
			case MOBILE_CONNECT_RESULT.MOBILE_CONNECT_RESULT_CHARACTER_STILL_IN_WORLD:
				reason = DisconnectReason.CharacterInWorld;
				break;
			case MOBILE_CONNECT_RESULT.MOBILE_CONNECT_RESULT_UNABLE_TO_ENTER_WORLD:
				reason = DisconnectReason.CantEnterWorld;
				break;
			case MOBILE_CONNECT_RESULT.MOBILE_CONNECT_RESULT_MOBILE_LOGIN_DISABLED:
				reason = DisconnectReason.LoginDisabled;
				break;
			case MOBILE_CONNECT_RESULT.MOBILE_CONNECT_RESULT_MOBILE_TRIAL_NOT_ALLOWED:
				reason = DisconnectReason.TrialNotAllowed;
				break;
			case MOBILE_CONNECT_RESULT.MOBILE_CONNECT_RESULT_MOBILE_CONSUMPTION_TIME:
				reason = DisconnectReason.ConsumptionTimeNotAllowed;
				break;
			}
			this.MobileDisconnect(reason);
		}
	}

	public void MobileLoginResult(bool success, int version)
	{
	}

	private void MobileLoggedIn()
	{
		Main.instance.MobileLoggedIn();
		this.m_lastPingTime = Time.get_timeSinceLevelLoad();
		this.m_lastPongTime = this.m_lastPingTime;
		this.m_pongReceived = false;
		this.SetLoginState(Login.eLoginState.MOBILE_LOGGED_IN_IDLE);
		AllPopups.instance.HideAllPopups();
		MobilePlayerLoginInfoForBI obj = new MobilePlayerLoginInfoForBI();
		Login.instance.SendToMobileServer(obj);
	}

	public void InitiateMobileDisconnect()
	{
		this.MobileDisconnect(DisconnectReason.Generic);
	}

	public bool IsMobileDisconnected()
	{
		return this.m_mobileNetwork == null || !this.m_mobileNetwork.IsConnected || (this.m_loginState == Login.eLoginState.MOBILE_DISCONNECTING || this.m_loginState == Login.eLoginState.MOBILE_DISCONNECTED || this.m_loginState == Login.eLoginState.MOBILE_DISCONNECTED_BY_SERVER || this.m_loginState == Login.eLoginState.MOBILE_DISCONNECTED_IDLE);
	}

	private void MobileDisconnect(DisconnectReason reason)
	{
		this.m_recentDisconnectReason = reason;
		if (this.m_mobileNetwork != null && this.m_mobileNetwork.IsConnected)
		{
			this.SetLoginState(Login.eLoginState.MOBILE_DISCONNECTING);
			this.m_mobileNetwork.Disconnect();
		}
		else
		{
			this.SetLoginState(Login.eLoginState.MOBILE_DISCONNECTED);
		}
	}

	private void MobileDisconnected()
	{
		this.LoginLog("Disconnected");
		AllPopups.instance.HideAllPopups();
		AllPanels.instance.ShowTitlePanel();
		this.SetLoginState(Login.eLoginState.MOBILE_DISCONNECTED_IDLE);
		string text = null;
		string text2 = null;
		switch (this.m_recentDisconnectReason)
		{
		case DisconnectReason.ConnectionLost:
		case DisconnectReason.PingTimeout:
			text2 = StaticDB.GetString("DISCONNECTED_BY_SERVER", null);
			break;
		case DisconnectReason.TimeoutContactingServer:
		case DisconnectReason.Generic:
			text2 = StaticDB.GetString("CANT_CONNECT", null);
			break;
		case DisconnectReason.AppVersionOld:
			text = StaticDB.GetString("UPDATE_REQUIRED", null);
			text2 = StaticDB.GetString("UPDATE_REQUIRED_DESCRIPTION", null);
			break;
		case DisconnectReason.AppVersionNew:
			text = StaticDB.GetString("INCOMPATIBLE_REALM", null);
			text2 = StaticDB.GetString("INCOMPATIBLE_REALM_DESCRIPTION", null);
			break;
		case DisconnectReason.CharacterInWorld:
			text2 = StaticDB.GetString("ALREADY_LOGGED_IN", null);
			break;
		case DisconnectReason.CantEnterWorld:
			text2 = StaticDB.GetString("CHARACTER_UNAVAILABLE", null);
			break;
		case DisconnectReason.LoginDisabled:
			text2 = StaticDB.GetString("LOGIN_UNAVAILABLE", null);
			break;
		case DisconnectReason.TrialNotAllowed:
			text2 = StaticDB.GetString("TRIAL_LOGIN_UNAVAILABLE", null);
			break;
		case DisconnectReason.ConsumptionTimeNotAllowed:
			text2 = StaticDB.GetString("SUBSCRIPTION_REQUIRED", null);
			break;
		}
		if (text != null && text2 != null)
		{
			AllPopups.instance.ShowGenericPopup(text, text2);
		}
		else if (text2 != null)
		{
			AllPopups.instance.ShowGenericPopupFull(text2);
		}
		this.m_recentDisconnectReason = DisconnectReason.None;
	}

	private void MobileDisconnectedByServer()
	{
		this.LoginLog("DisconnectedByServer");
		AllPopups.instance.HideAllPopups();
		AllPanels.instance.ShowTitlePanel();
		this.SetLoginState(Login.eLoginState.MOBILE_DISCONNECTED_IDLE);
		if (this.m_currentServerProtocolVersion >= 0)
		{
			if (this.m_currentServerProtocolVersion > this.m_expectedServerProtocolVersion)
			{
				AllPopups.instance.ShowGenericPopup(StaticDB.GetString("UPDATE_REQUIRED", null), StaticDB.GetString("UPDATE_REQUIRED_DESCRIPTION", null));
			}
			else if (this.m_currentServerProtocolVersion < this.m_expectedServerProtocolVersion)
			{
				AllPopups.instance.ShowGenericPopup(StaticDB.GetString("INCOMPATIBLE_REALM", null), StaticDB.GetString("INCOMPATIBLE_REALM_DESCRIPTION", null));
			}
			else
			{
				AllPopups.instance.ShowGenericPopupFull(StaticDB.GetString("DISCONNECTED_BY_SERVER", null));
			}
		}
		else
		{
			AllPopups.instance.ShowGenericPopupFull(StaticDB.GetString("DISCONNECTED_BY_SERVER", null));
		}
	}

	public void MobileConnectDestroy()
	{
		if (this.m_mobileNetwork != null && this.m_mobileNetwork.IsConnected)
		{
			this.LoginLog("MobileConnectDestroy(): Disconnecting.");
			this.m_mobileNetwork.Disconnect();
		}
	}

	public void SendToMobileServer(object obj)
	{
		switch (this.m_loginState)
		{
		case Login.eLoginState.MOBILE_LOGGING_IN:
		case Login.eLoginState.MOBILE_LOGGED_IN:
		case Login.eLoginState.MOBILE_LOGGED_IN_IDLE:
			this.m_mobileNetwork.SendMessage(obj);
			break;
		default:
			Debug.Log("SendToMobileServer: Ignored message send while in state " + this.m_loginState.ToString() + ", message: " + obj.ToString());
			break;
		}
	}

	public void BackToAccountSelect()
	{
		List<EntityId> gameAccountList = BattleNet.GetGameAccountList();
		if (gameAccountList.get_Count() > 1 && this.m_loginState == Login.eLoginState.BN_CHARACTER_LIST_WAIT)
		{
			this.m_useCachedWoWAccount = false;
			this.BnLoginStart(true, false, false, false);
		}
		else
		{
			BattleNet.Get().RequestCloseAurora();
			BattleNet.ProcessAurora();
			this.BnErrorsUpdate();
			AllPanels.instance.ShowTitlePanel();
			this.SetLoginState(Login.eLoginState.IDLE);
			this.m_useCachedWoWAccount = true;
		}
	}

	public void BackToTitle()
	{
		BattleNet.Get().RequestCloseAurora();
		BattleNet.ProcessAurora();
		this.BnErrorsUpdate();
		this.SetLoginState(Login.eLoginState.IDLE);
		AllPanels.instance.ShowTitlePanel();
	}

	public void CancelLogin()
	{
		switch (this.m_loginState)
		{
		case Login.eLoginState.WEB_AUTH_START:
		case Login.eLoginState.WEB_AUTH_LOADING:
		case Login.eLoginState.WEB_AUTH_IN_PROGRESS:
		case Login.eLoginState.WEB_AUTH_FAILED:
		case Login.eLoginState.BN_LOGIN_START:
		case Login.eLoginState.BN_LOGIN_WAIT_FOR_LOGON:
		case Login.eLoginState.BN_LOGIN_PROVIDE_TOKEN:
		case Login.eLoginState.BN_LOGGING_IN:
		case Login.eLoginState.BN_TICKET_WAIT:
		case Login.eLoginState.BN_SUBREGION_LIST_WAIT:
		case Login.eLoginState.BN_CHARACTER_LIST_WAIT:
		case Login.eLoginState.BN_REALM_JOIN_WAIT:
		case Login.eLoginState.BN_LOGIN_UNKNOWN:
			BattleNet.Get().RequestCloseAurora();
			BattleNet.ProcessAurora();
			this.BnErrorsUpdate();
			this.SetLoginState(Login.eLoginState.IDLE);
			AllPanels.instance.ShowTitlePanel();
			break;
		case Login.eLoginState.MOBILE_CONNECT:
		case Login.eLoginState.MOBILE_CONNECTING:
		case Login.eLoginState.MOBILE_CONNECT_FAILED:
		case Login.eLoginState.MOBILE_DISCONNECTING:
		case Login.eLoginState.MOBILE_DISCONNECTED:
		case Login.eLoginState.MOBILE_DISCONNECTED_BY_SERVER:
		case Login.eLoginState.MOBILE_DISCONNECTED_IDLE:
		case Login.eLoginState.MOBILE_LOGGING_IN:
		case Login.eLoginState.MOBILE_LOGGED_IN:
		case Login.eLoginState.MOBILE_LOGGED_IN_IDLE:
			AllPanels.instance.ShowOrderHallMultiPanel(false);
			Login.instance.ReconnectToMobileServerCharacterSelect();
			break;
		}
	}

	public int GetExpectedServerProtocolVersion()
	{
		return this.m_expectedServerProtocolVersion;
	}

	private void InitRecentCharacters()
	{
		this.m_recentCharacters = new List<RecentCharacter>();
	}

	private void LoadRecentCharacters()
	{
		for (int i = 0; i < 3; i++)
		{
			string @string = SecurePlayerPrefs.GetString("RecentCharacter" + i, Main.uniqueIdentifier);
			if (@string != null && @string != string.Empty)
			{
				object obj = MessageFactory.Deserialize(@string);
				if (obj is RecentCharacter)
				{
					RecentCharacter recentCharacter = (RecentCharacter)obj;
					if (recentCharacter.Version == 1)
					{
						this.m_recentCharacters.Add(recentCharacter);
					}
					else
					{
						PlayerPrefs.DeleteKey("RecentCharacter" + i);
					}
				}
			}
		}
	}

	private void SaveRecentCharacters()
	{
		int i = 0;
		while (i < this.m_recentCharacters.get_Count() && i < 3)
		{
			string value = MessageFactory.Serialize(this.m_recentCharacters.get_Item(i));
			SecurePlayerPrefs.SetString("RecentCharacter" + i, value, Main.uniqueIdentifier);
			i++;
		}
		while (i < 3)
		{
			SecurePlayerPrefs.DeleteKey("RecentCharacter" + i);
			i++;
		}
		PlayerPrefs.Save();
	}

	private void UpdateRecentCharacters()
	{
		RecentCharacter recentCharacter = new RecentCharacter();
		recentCharacter.Entry = this.m_selectedCharacterEntry;
		recentCharacter.GameAccount = this.m_gameAccount;
		recentCharacter.UnixTime = GeneralHelpers.CurrentUnixTime();
		recentCharacter.WebToken = this.m_webToken;
		recentCharacter.SubRegion = this.m_subRegion;
		recentCharacter.Version = 1;
		RecentCharacter recentCharacter2 = null;
		using (List<RecentCharacter>.Enumerator enumerator = this.m_recentCharacters.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				RecentCharacter current = enumerator.get_Current();
				if (current.Entry.PlayerGuid == recentCharacter.Entry.PlayerGuid)
				{
					recentCharacter2 = current;
				}
			}
		}
		if (recentCharacter2 != null)
		{
			this.m_recentCharacters.Remove(recentCharacter2);
			this.m_recentCharacters.Add(recentCharacter);
		}
		else
		{
			RecentCharacter recentCharacter3 = null;
			int num = GeneralHelpers.CurrentUnixTime();
			using (List<RecentCharacter>.Enumerator enumerator2 = this.m_recentCharacters.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					RecentCharacter current2 = enumerator2.get_Current();
					if (current2.UnixTime < num)
					{
						num = current2.UnixTime;
						recentCharacter3 = current2;
					}
				}
			}
			if (recentCharacter3 != null && this.m_recentCharacters.get_Count() >= 3)
			{
				this.m_recentCharacters.Remove(recentCharacter3);
			}
			this.m_recentCharacters.Add(recentCharacter);
		}
		this.SaveRecentCharacters();
		int i = 0;
		int num2 = 0;
		while (num2 < this.m_recentCharacters.get_Count() && num2 < 3)
		{
			if (this.m_recentCharacters.get_Item(num2).Entry.PlayerGuid != this.m_selectedCharacterEntry.PlayerGuid)
			{
				AllPanels.instance.SetRecentCharacter(i++, this.m_recentCharacters.get_Item(num2));
			}
			num2++;
		}
		while (i < 3)
		{
			AllPanels.instance.SetRecentCharacter(i, null);
			i++;
		}
	}

	private void CheckRecentCharacters(JamJSONCharacterEntry[] characters, string subRegion)
	{
		List<RecentCharacter> list = new List<RecentCharacter>();
		using (List<RecentCharacter>.Enumerator enumerator = this.m_recentCharacters.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				RecentCharacter current = enumerator.get_Current();
				if (current.SubRegion == subRegion)
				{
					JamJSONCharacterEntry jamJSONCharacterEntry = null;
					for (int i = 0; i < characters.Length; i++)
					{
						JamJSONCharacterEntry jamJSONCharacterEntry2 = characters[i];
						if (jamJSONCharacterEntry2.PlayerGuid == current.Entry.PlayerGuid)
						{
							jamJSONCharacterEntry = jamJSONCharacterEntry2;
							break;
						}
					}
					if (jamJSONCharacterEntry == null)
					{
						this.LoginLog("Removing recent character " + current.Entry.Name + ", not found in character list.");
						list.Add(current);
					}
					else if (jamJSONCharacterEntry.Name != current.Entry.Name)
					{
						this.LoginLog("Removing recent character " + current.Entry.Name + ", name doesn't match " + jamJSONCharacterEntry.Name);
						list.Add(current);
					}
					else if (jamJSONCharacterEntry.VirtualRealmAddress != current.Entry.VirtualRealmAddress)
					{
						this.LoginLog(string.Concat(new object[]
						{
							"Removing recent character ",
							current.Entry.Name,
							", realm address changed from ",
							current.Entry.VirtualRealmAddress,
							" to ",
							jamJSONCharacterEntry.VirtualRealmAddress
						}));
						list.Add(current);
					}
				}
			}
		}
		using (List<RecentCharacter>.Enumerator enumerator2 = list.GetEnumerator())
		{
			while (enumerator2.MoveNext())
			{
				RecentCharacter current2 = enumerator2.get_Current();
				this.m_recentCharacters.Remove(current2);
			}
		}
		this.SaveRecentCharacters();
	}
}
