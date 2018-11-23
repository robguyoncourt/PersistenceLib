using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.UnitTests
{
	[TestClass]
	public class TestPersistSQLServer
	{

		[TestMethod]
		public async Task TestSmallTestFileWriteAsync()
		{
			// arrange
			TestHelper helper = new TestHelper();
			IDBConnection conn = new SQLServerADOConnection();

			PersistenceDB persistSQL = new PersistenceDB("dbo", new List<(string key, string name)>() { ("order_id", "orders"), ("parameter", "minerva_params"), ("control_id", "control_messages") }, conn);
			PersistenceService ps = new PersistenceService(x => { return x; }, persistSQL, 
					new Dictionary<string, string>() {
					{ DBConnectionParams.DATABASE,"Persistence"},
					{ DBConnectionParams.SERVER,"DESKTOP-EP61E82"},
					{ DBConnectionParams.USERID, "ADOUSER"},
					{ DBConnectionParams.PASSWORD,"latentzero" } });

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
				//persistSQL?.Dispose();
				//TestHelper.CleanupAfterTest(ps, tempFile, subscription);
			}
		}

		[TestMethod]
		public async Task TestBigFile()
		{
			// arrange
			TestHelper helper = new TestHelper();
			PersistenceDB persistSQL = helper.GetAndInitLocalSqlServer();
			PersistenceService ps = new PersistenceService(x => { return x; }, persistSQL, null);
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
