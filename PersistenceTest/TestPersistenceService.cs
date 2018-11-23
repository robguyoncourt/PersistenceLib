using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Persistence.UnitTests
{
	[TestClass]
	public class TestPersistenceService
	{
		[TestMethod]
		public async Task TestSmallTestFileWriteAsync()
		{
			// arrange
			TestHelper helper = new TestHelper();
			PersistenceService ps = helper.GetTestPersistenceService();
			int count = 0;
			bool complete = false;
			string tempFile = string.Empty;
			IDisposable subscription = Disposable.Empty;

			// act
			try
			{
				subscription = ps.TransactionElementsSource.Subscribe(x => count += x.Elements.Count, () => complete = true);

				tempFile = helper.CreateTemporaryFileWithContent(helper.GetXMLFileAsString(helper.SMALLTESTFILE));

				await ps.Start(tempFile);

				// verify
				Assert.IsTrue(complete);
				Assert.AreEqual(5, count);

			}
			finally
			{
				TestHelper.CleanupAfterTest(ps, tempFile, subscription);
			}
		}

		[TestMethod]
		public async Task TestMedTestFileSwitch1Write()
		{
			// arrange
			TestHelper helper = new TestHelper();
			PersistenceService ps = new PersistenceService(x => helper.CreateTemporaryFileWithContent(helper.GetXMLFileAsString(x)), new TestHelper.TestPersistenceDestination(), null);
			int count = 0;
			bool complete = false;
			string tempFile = string.Empty;
			IDisposable subscription = Disposable.Empty;

			//act
			try
			{
				subscription = ps.TransactionElementsSource.Subscribe(x => count += x.Elements.Count, () => complete = true);

				tempFile = helper.CreateTemporaryFileWithContent(helper.GetXMLFileAsString(helper.MEDIUMTESTSWITCH1FILE));

				await ps.Start(tempFile);

			//verify
				Assert.IsTrue(complete);
				Assert.AreEqual(64, count);
			}
			finally
			{
				TestHelper.CleanupAfterTest(ps, tempFile, subscription);
			}
		}

		[TestMethod]
		public async Task TestMedTestFileWriteFromTransaction()
		{
			// arrange
			TestHelper helper = new TestHelper();
			PersistenceService ps = helper.GetTestPersistenceService();
			int count = 0;
			bool complete = false;
			string tempFile = string.Empty;
			IDisposable subscription = Disposable.Empty;

			// act
			try
			{
				subscription = ps.TransactionElementsSource.Subscribe(x => count += x.Elements.Count, () => complete = true);

				tempFile = helper.CreateTemporaryFileWithContent(helper.GetXMLFileAsString(helper.MEDIUMTESTFILE));

				await ps.StartFromTransaction(tempFile, 32);

			// verify
				Assert.IsTrue(complete);
				Assert.AreEqual(24, count);
			}
			finally
			{
				TestHelper.CleanupAfterTest(ps, tempFile, subscription);
			}
		}

		[TestMethod]
		public async Task TestBigFile()
		{
			//arrange
			TestHelper helper = new TestHelper();
			PersistenceService ps = helper.GetTestPersistenceService();
			int count = 0;
			bool complete = false;
			string tempFile = string.Empty;
			IDisposable subscription = Disposable.Empty;

			// act
			try
			{
				subscription = ps.TransactionElementsSource.Subscribe(x => count += x.Elements.Count, () => complete = true);

				tempFile = helper.CreateTemporaryFileWithContent(helper.GetXMLFileAsString(helper.BFFTEST));

				await ps.Start(tempFile);
				
			// verify
				Assert.IsTrue(complete);
				Assert.AreEqual(113699, count);
			}
			finally
			{
				TestHelper.CleanupAfterTest(ps, tempFile, subscription);

			}
		}


	}
}
