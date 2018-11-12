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
			PersistSQLServer persistSQL = new PersistSQLServer("dbo", new List<(string key, string name)>() { ("order_id", "orders"), ("parameter", "minerva_params"), ("control_id", "control_messages") });
			persistSQL.Connect(new Dictionary<string, string>());
			PersistenceService ps = new PersistenceService(x => { return x; }, persistSQL);
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
	}
}
