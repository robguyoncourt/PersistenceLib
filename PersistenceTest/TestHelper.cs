using System.IO;
using System.Reflection;
using System.Text;
using System.IO.Compression;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Persistence.UnitTests
{
	public class TestHelper
	{
		public readonly string SMALLTESTFILE = "SmallTestData.xml";
		public readonly string MEDIUMTESTFILE = "MedTestData.xml";
		public readonly string MEDIUMTESTSWITCH1FILE = "MedTestDataSwitch1.xml";
		public readonly string MEDIUMTESTSWITCH2FILE = "MedTestDataSwitch2.xml";
		public readonly string BFFTEST = "BFF.xml.gz";

		public static HashSet<string> IgnoreElements = new HashSet<string>
			{
				"dealing_notify",
				"desk_notify" ,
				"investment_notify" ,
				"action" ,
				"transaction_tag" ,
				"summary_last_liquidity_display",
			};


		public static PersistenceDB GetTestPersistenceDBForOrders(IDBConnection dbConn)
		{
			PersistenceDB db = new PersistenceDB("dbo", new List<(string key, string name)>() { ("order_id", "orders") }, dbConn);
			db.Connect(new Dictionary<string, string>()
			{{ DBConnectionParams.DATABASE,"Persistence"},
			{ DBConnectionParams.SERVER,"DESKTOP-EP61E82"},
			{ DBConnectionParams.USERID, "ADOUSER"},
			{ DBConnectionParams.PASSWORD,"latentzero" } });

			return db;
		}

		public PersistenceService GetTestPersistenceService()
		{
			return new PersistenceService(x => { return x; }, new TestPersistenceDestination(), null);
		}

		public PersistenceDB GetAndInitLocalSqlServer()
		{
			PersistenceDB persistSQL = new PersistenceDB("dbo", new List<(string key, string name)>() {
				("control_id","control_messages"),
				("merge_id","execution_merges"),
				("execution_id","executions"),
				("pairoff_id","pairoffs"),
				("audit_event_id","audit_events"),
				("release_id","releases"),
				("allocation_id", "allocations"),
				("strategy_id", "strategies"),
				("order_id", "orders"),
				("list_id", "list_orders"),
				("market_list_id", "market_lists"),
				("quote_id", "quotes"),
				("quote_list_id", "quote_lists"),
				("contingency_group_id", "contingency_groups"),
				("contingency_link_Id", "contingency_links"),
				("charge_id", "charge_details"),
				("collateral_id", "collaterals"),
				("parameter", "minerva_params")
			}, new DBTestConnection());

			//persistSQL.Connect(new Dictionary<string, string>() {
			//		{ persistSQL.DBCONKEY_DATABASE,"Persistence"},
			//		{ persistSQL.DBCONKEY_SERVER,"DESKTOP-EP61E82"},
			//		{ persistSQL.DBCONKEY_USERID, "ADOUSER"},
			//		{ persistSQL.DBCONKEY_PASSWORD,"latentzero"}
			//	});

			//persistSQL.RunClearDown();

			return persistSQL;

		}

		public string GetResourceTextFile(string filename)
		{
			return CreateTemporaryFileWithContent(GetXMLFileAsString(filename));
		}

		public static Stream GetManifestFileAsStream(string filename)
		{
			return Assembly.GetExecutingAssembly().GetManifestResourceStream("PersistenceTest." + filename);
		}

		public string GetXMLFileAsString(string filename)
		{
			string retVal = string.Empty;

			using (Stream stream = GetManifestFileAsStream(filename))
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

		public class TestPersistenceDestination : IPersistDestination
		{
			public bool IsConnected => true;

			public void Connect(Dictionary<string, string> connectionParams)
			{}

			public void Disconnect()
			{}

			public void Dispose()
			{
			}

			public Task Persist(TransactionElements tes)
			{
				return Task.CompletedTask;
			}
		}
	}
}
