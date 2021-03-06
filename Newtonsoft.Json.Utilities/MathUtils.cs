using System;

namespace Newtonsoft.Json.Utilities
{
	internal class MathUtils
	{
		public static int IntLength(int i)
		{
			if (i < 0)
			{
				throw new ArgumentOutOfRangeException();
			}
			if (i == 0)
			{
				return 1;
			}
			return (int)Math.Floor(Math.Log10((double)i)) + 1;
		}

		public static int HexToInt(char h)
		{
			if (h >= '0' && h <= '9')
			{
				return (int)(h - '0');
			}
			if (h >= 'a' && h <= 'f')
			{
				return (int)(h - 'a' + '\n');
			}
			if (h >= 'A' && h <= 'F')
			{
				return (int)(h - 'A' + '\n');
			}
			return -1;
		}

		public static char IntToHex(int n)
		{
			if (n <= 9)
			{
				return (char)(n + 48);
			}
			return (char)(n - 10 + 97);
		}

		public static int GetDecimalPlaces(double value)
		{
			int num = 10;
			double num2 = Math.Pow(0.1, (double)num);
			if (value == 0.0)
			{
				return 0;
			}
			int num3 = 0;
			while (value - Math.Floor(value) > num2 && num3 < num)
			{
				value *= 10.0;
				num3++;
			}
			return num3;
		}

		public static int? Min(int? val1, int? val2)
		{
			if (!val1.get_HasValue())
			{
				return val2;
			}
			if (!val2.get_HasValue())
			{
				return val1;
			}
			return new int?(Math.Min(val1.get_Value(), val2.get_Value()));
		}

		public static int? Max(int? val1, int? val2)
		{
			if (!val1.get_HasValue())
			{
				return val2;
			}
			if (!val2.get_HasValue())
			{
				return val1;
			}
			return new int?(Math.Max(val1.get_Value(), val2.get_Value()));
		}

		public static double? Min(double? val1, double? val2)
		{
			if (!val1.get_HasValue())
			{
				return val2;
			}
			if (!val2.get_HasValue())
			{
				return val1;
			}
			return new double?(Math.Min(val1.get_Value(), val2.get_Value()));
		}

		public static double? Max(double? val1, double? val2)
		{
			if (!val1.get_HasValue())
			{
				return val2;
			}
			if (!val2.get_HasValue())
			{
				return val1;
			}
			return new double?(Math.Max(val1.get_Value(), val2.get_Value()));
		}

		public static bool ApproxEquals(double d1, double d2)
		{
			return Math.Abs(d1 - d2) < Math.Abs(d1) * 1E-06;
		}
	}
}
