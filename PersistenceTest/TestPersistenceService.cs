using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Persistence.UnitTests
{
	[TestClass]
	public class TestPersistenceService
	{
		[TestMethod]
		public void TestSmallTestFileWrite()
		{
			TestHelper helper = new TestHelper();
			PersistenceService ps = new PersistenceService();
			int count = 0;
			bool complete = false;
			string tempFile = string.Empty;
			IDisposable subscription =  ps.TransactionElementsSource.Subscribe(x => count += x.Elements.Count, () => complete = true);

			try
			{
				tempFile = helper.CreateTemporaryFileWithContent(helper.GetXMLFileAsString(helper.SMALLTESTFILE));

				ps.Start(tempFile).Wait();

				Assert.IsTrue(complete);
				Assert.AreEqual(5, count);
			}
			finally
			{
				ps.Stop();
				subscription.Dispose();
				GC.WaitForPendingFinalizers();
			}
		}

		[TestMethod]
		public void TestMedTestFileSwitch1Write()
		{
			TestHelper helper = new TestHelper();
			PersistenceService ps = new PersistenceService(x => helper.CreateTemporaryFileWithContent(helper.GetXMLFileAsString(x)));
			int count = 0;
			bool complete = false;
			string tempFile = string.Empty;
			IDisposable subscription = ps.TransactionElementsSource.Subscribe(x => count += x.Elements.Count, () => complete = true);

			try
			{
				tempFile = helper.CreateTemporaryFileWithContent(helper.GetXMLFileAsString(helper.MEDIUMTESTSWITCH1FILE));

				ps.Start(tempFile).Wait();

				Assert.IsTrue(complete);
				Assert.AreEqual(64, count);
			}
			finally
			{
				ps.Stop();
				subscription.Dispose();
				GC.WaitForPendingFinalizers();
			}
		}

		[TestMethod]
		public void TestMedTestFileWriteFromTransation()
		{
			TestHelper helper = new TestHelper();
			PersistenceService ps = new PersistenceService();
			int count = 0;
			bool complete = false;
			string tempFile = string.Empty;
			IDisposable subscription = ps.TransactionElementsSource.Subscribe(x => count += x.Elements.Count, () => complete = true);

			try
			{
				tempFile = helper.CreateTemporaryFileWithContent(helper.GetXMLFileAsString(helper.MEDIUMTESTFILE));

				ps.StartFromTransaction(tempFile, 32).Wait();

				Assert.IsTrue(complete);
				Assert.AreEqual(24, count);
			}
			finally
			{
				ps.Stop();
				subscription.Dispose();
				GC.WaitForPendingFinalizers();
			}
		}

	}
}
