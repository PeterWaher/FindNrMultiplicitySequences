using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace FindNrMultiplicitySequences
{
	class Program
	{
		private const int MaxSum = 10000;

		private static BigInteger[,] cache = new BigInteger[MaxSum, MaxSum];
		private static LinkedList<int> todo = new LinkedList<int>();
		private static SortedDictionary<int, Result> result = new SortedDictionary<int, Result>();
		private static object synchObject = new object();

		public static BigInteger FindNrMultiplicitySequences(int SumM)
		{
			return FindNrMultiplicitySequences(SumM, SumM);
		}

		public static BigInteger FindNrMultiplicitySequences(int SumM, int MaxMultiplicity)
		{
			BigInteger NrSequences;
			int i;

			if (MaxMultiplicity > SumM)
				MaxMultiplicity = SumM;

			NrSequences = cache[SumM - 1, MaxMultiplicity - 1];
			if (NrSequences != 0)
				return NrSequences;

			if (MaxMultiplicity > 0)
			{
				for (i = 1; i <= MaxMultiplicity; i++)
				{
					if (i < SumM)
						NrSequences += FindNrMultiplicitySequences(SumM - i, i);
					else
						NrSequences++;
				}
			}

			cache[SumM - 1, MaxMultiplicity - 1] = NrSequences;

			return NrSequences;
		}

		private class Result
		{
			public int SumM;
			public BigInteger N;
			public double ms;

			public Result(int SumM, BigInteger N, double ms)
			{
				this.SumM = SumM;
				this.N = N;
				this.ms = ms;
			}
		}

		static void Main(string[] args)
		{
			int SumM;

			LoadCache();

			for (SumM = 1; SumM <= MaxSum; SumM++)
				todo.AddLast(SumM);

			List<ManualResetEvent> Dones = new List<ManualResetEvent>();

			for (int i = 1; i <= System.Environment.ProcessorCount; i++)
			{
				ManualResetEvent Done = new ManualResetEvent(false);
				Thread T = new Thread(Calculator);
				T.Name = "Calculator" + i.ToString();
				T.Priority = ThreadPriority.Normal;
				T.Start(Done);

				Dones.Add(Done);
			}

			WaitHandle.WaitAll(Dones.ToArray());

			if (File.Exists("NrMultiplicitySequences.tsv.bak"))
				File.Delete("NrMultiplicitySequences.tsv.bak");

			if (File.Exists("NrMultiplicitySequences.tsv"))
				File.Move("NrMultiplicitySequences.tsv", "NrMultiplicitySequences.tsv.bak");

			using (StreamWriter w = File.CreateText("NrMultiplicitySequences.tsv"))
			{
				Console.Out.WriteLine("SumM\tNrSequences\t\tms");
				w.WriteLine("SumM\tNrSequences\tP\tms");

				Result Prev = null;
				string P;

				foreach (Result Result in result.Values)
				{
					if (Prev != null)
						P = Div(Result.N, Prev.N, 20);
					else
						P = string.Empty;

					Output(Console.Out, Result, P);
					Output(w, Result, P);

					Prev = Result;
				}

				w.Flush();
			}

			if (File.Exists("NrMultiplicitySequences.script.bak"))
				File.Delete("NrMultiplicitySequences.script.bak");

			if (File.Exists("NrMultiplicitySequences.script"))
				File.Move("NrMultiplicitySequences.script", "NrMultiplicitySequences.script.bak");

			using (StreamWriter w = File.CreateText("NrMultiplicitySequences.script"))
			{
				w.Write("M3:=[");

				Result Prev = null;
				string P;

				foreach (Result Result in result.Values)
				{
					if (Prev != null)
					{
						w.WriteLine(",");
						P = Div(Result.N, Prev.N, 20);
					}
					else
						P = "null";

					w.Write('[');
					w.Write(Result.SumM);
					w.Write(',');
					w.Write(Result.N);
					w.Write(',');
					w.Write(P);
					w.Write(',');
					w.Write(Result.ms.ToString().Replace(System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, "."));
					w.Write(']');

					Prev = Result;
				}

				w.WriteLine("];");
				w.Flush();
			}

			SaveCache();
		}

		static void Output(TextWriter w, Result Result, string P)
		{
			w.Write(Result.SumM);
			w.Write("\t");
			w.Write(Result.N);
			w.Write("\t");
			w.Write(P);
			w.Write("\t");
			w.WriteLine(Result.ms.ToString().Replace(System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, "."));
		}

		static void LoadCache()
		{
			if (File.Exists("NrMultiplicitySequences.cache"))
			{
				using (FileStream fs = File.OpenRead("NrMultiplicitySequences.cache"))
				{
					using (BinaryReader r = new BinaryReader(fs))
					{
						int MaxSumCache = r.ReadInt32();
						BigInteger k;
						byte[] Bin;
						int l;
						int Offset;
						byte b;

						for (int i = 0; i < MaxSumCache; i++)
						{
							for (int j = 0; j < MaxSumCache; j++)
							{
								Offset = 0;
								b = r.ReadByte();
								l = b & 127;
								while ((b & 128) != 0)
								{
									b = r.ReadByte();
									Offset += 7;
									l |= ((b & 127) << Offset);
								}

								if (l > 0)
								{
									Bin = r.ReadBytes(l);

									if (i < MaxSum && j < MaxSum)
									{
										k = new BigInteger(Bin);
										cache[i, j] = k;
									}
								}
							}
						}
					}
				}
			}
		}

		static void SaveCache()
		{
			using (FileStream fs = File.Create("NrMultiplicitySequences.cache"))
			{
				using (BinaryWriter w = new BinaryWriter(fs))
				{
					w.Write(MaxSum);

					for (int i = 0; i < MaxSum; i++)
					{
						for (int j = 0; j < MaxSum; j++)
						{
							BigInteger k = cache[i, j];
							byte[] Bin = k.ToByteArray();
							int l = Bin.Length;
							byte b;

							if ((b = (byte)l) == 0)
								w.Write(b);
							else
							{
								while (l >= 128)
								{
									w.Write(b | 128);
									l >>= 7;
									b = (byte)l;
								}

								w.Write(b);
								w.Write(Bin, 0, Bin.Length);
							}
						}
					}
				}
			}
		}

		static void Calculator(object P)
		{
			ManualResetEvent Done = (ManualResetEvent)P;
			Stopwatch Clock = new Stopwatch();
			BigInteger N;
			double ms;
			int SumM;

			while (true)
			{
				lock (synchObject)
				{
					if (todo.First == null)
						break;

					SumM = todo.First.Value;
					todo.RemoveFirst();
				}

				Clock.Start();
				N = FindNrMultiplicitySequences(SumM);
				Clock.Stop();
				ms = Clock.ElapsedTicks;
				ms /= Stopwatch.Frequency;
				ms *= 1000;

				lock (synchObject)
				{
					result[SumM] = new Result(SumM, N, ms);

					if (SumM % 100 == 0)
						Console.Out.WriteLine(SumM + ", " + N.ToString());
				}
			}

			Done.Set();
		}

		static string Div(BigInteger Num, BigInteger Denom, int MaxDecimals)
		{
			StringBuilder Result = new StringBuilder();

			BigInteger WholePart = Num / Denom;
			Result.Append(WholePart.ToString());

			Num -= Denom * WholePart;

			if (Num != 0)
			{
				Result.Append('.');

				while (Num != 0 && MaxDecimals > 0)
				{
					Num *= 10;
					WholePart = Num / Denom;
					Result.Append(WholePart.ToString());
					Num -= Denom * WholePart;
					MaxDecimals--;
				}
			}

			return Result.ToString();
		}
	}
}
