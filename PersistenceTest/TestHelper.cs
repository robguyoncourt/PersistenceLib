using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using System.Reflection;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.UnitTests
{
	public class TestHelper
	{
		public readonly string SMALLTESTFILE = "SmallTestData.xml";
		public readonly string MEDIUMTESTFILE = "MedTestData.xml";
		public readonly string MEDIUMTESTSWITCH1FILE = "MedTestDataSwitch1.xml";
		public readonly string MEDIUMTESTSWITCH2FILE = "MedTestDataSwitch2.xml";


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
				retVal = new StreamReader(stream).ReadToEnd();
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
			}

			return fileName;
		}

		public FileStream CreateTemporaryFileStream()
		{
			string tempFile = Path.GetTempFileName();

			return new FileStream(tempFile, FileMode.Open);

		}
	}
}
