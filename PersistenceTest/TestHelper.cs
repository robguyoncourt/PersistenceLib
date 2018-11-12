using System.IO;
using System.Reflection;
using System.Text;
using System.IO.Compression;
using System;

namespace Persistence.UnitTests
{
	public class TestHelper
	{
		public readonly string SMALLTESTFILE = "SmallTestData.xml";
		public readonly string MEDIUMTESTFILE = "MedTestData.xml";
		public readonly string MEDIUMTESTSWITCH1FILE = "MedTestDataSwitch1.xml";
		public readonly string MEDIUMTESTSWITCH2FILE = "MedTestDataSwitch2.xml";
		public readonly string BFFTEST = "BFF.xml.gz";

		public string GetResourceTextFile(string filename)
		{
			return CreateTemporaryFileWithContent(GetXMLFileAsString(filename));
		}

		public string GetXMLFileAsString(string filename)
		{
			string retVal = string.Empty;

			using (Stream stream = Assembly.GetExecutingAssembly().
			 GetManifestResourceStream("PersistenceTest." + filename))
			{
				if (filename.EndsWith(".gz"))
				{
					using (GZipStream decompressionStream = new GZipStream(stream, CompressionMode.Decompress))
					{
						retVal = new StreamReader(decompressionStream).ReadToEnd();
					}
				}
				else
				{
					retVal = new StreamReader(stream).ReadToEnd();
				}
			}

			return retVal;
		}

		public string CreateTemporaryFileWithContent(string content)
		{
			string fileName = string.Empty;
			using (FileStream fs = CreateTemporaryFileStream())
			{
				var charBuffer = Encoding.UTF8.GetBytes(content);
				fs.Write(charBuffer, 0, charBuffer.Length);
				fileName = fs.Name;
				fs.Close();
			}

			return fileName;
		}

		public FileStream CreateTemporaryFileStream()
		{
			string tempFile = Path.GetTempFileName();

			return new FileStream(tempFile, FileMode.Open, FileAccess.ReadWrite);

		}

		public static void CleanupAfterTest(PersistenceService ps, string tempFile, IDisposable subscription)
		{
			ps.Stop();
			subscription.Dispose();
			GC.WaitForPendingFinalizers();
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}
}
