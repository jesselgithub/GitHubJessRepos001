using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using DbRef;
using UpTestDater.Data;

namespace UpTestDater
{
    class Program
    {
        public static string ConnectionString = @"data source=AAEINBR140874L;initial catalog=PEUTEST002;integrated security=True";

        static void Main(string[] args)
        {
            try
            {


                using (PEUTEST002Entities dbc = new PEUTEST002Entities())
                {
                    //Dictionary<int, string> dictBuildDefinitions = (from x in dbc.BuildDefinitions
                    //    select x).ToDictionary(y => y.BuildDefinitionId, x => x.BuildDefinitionName);

                    //Dictionary<int, string> dictBuildInstances = (from x in dbc.BuildInstances
                    //    select x).ToDictionary(x => x.BuildInstanceId, y => y.BuildInstanceName);

                    DatabaseInstance.BuildInstances = (from x in dbc.BuildInstances
                        select x).ToList();

                    DatabaseInstance.TestDates = (from x in dbc.TestDates
                        select x).ToList();

                    //DatabaseInstance.Assemblies = (from x in dbc.Assemblies
                    //    select x).ToList();

                    //DatabaseInstance.TestReports = (from x in dbc.TestReports
                    //    select x).ToList();


                    foreach (TiaVersion tiaVersion in dbc.TiaVersions.ToList())
                    {

                        foreach (BuildDefinition buildDefinition in (from x in dbc.BuildDefinitions
                            where x.VersionName == tiaVersion.VersionName
                            select x).ToList())
                        {
                            //var resultpath = Path.Combine(buildDefinition.ResultsShare, buildDefinition.BuildDefinitionName);
                            int count = 0;
                            foreach (string build in Directory.GetDirectories(buildDefinition.ResultsShare, buildDefinition.BuildDefinitionPattern, SearchOption.TopDirectoryOnly).Reverse())
                            {
                                ProcessBuildInstance(build, dbc, buildDefinition);
                                if (++count >= 2)
                                {
                                    break;
                                }
                            }
                        }
                    }


                    UpdateIds(dbc);

                    UpdateTableAssembly(dbc);
                    UpdateTableFixture(dbc);
                    UpdateTableTestcase(dbc);
                    UpdateTableTestReport(dbc);
                }
            }
            catch (Exception exception)
            {
                WriteException(exception);
            }
        }

        private static void UpdateTableTestReport(PEUTEST002Entities dbc)
        {
            DataTable dt_reports = new DataTable();
            dt_reports.Columns.Add(new DataColumn("BuildInstanceId", typeof(int)));
            dt_reports.Columns.Add(new DataColumn("MachineName", typeof(string)));
            dt_reports.Columns.Add(new DataColumn("Success", typeof(bool)));
            dt_reports.Columns.Add(new DataColumn("Executed", typeof(bool)));
            dt_reports.Columns.Add(new DataColumn("Time", typeof(int)));
            dt_reports.Columns.Add(new DataColumn("Message", typeof(string)));
            dt_reports.Columns.Add(new DataColumn("StackTrace", typeof(string)));
            dt_reports.Columns.Add(new DataColumn("TestcaseId", typeof(int)));

            foreach (LocalReport rpt in LocalCollection.AllLocalReports)
            {
                if (rpt.MachineName.Length > 150)
                {
                    rpt.MachineName = rpt.MachineName.Substring(0, 150);
                }
                DataRow dr = dt_reports.NewRow();
                dr["BuildInstanceId"] = rpt.BuildInstanceId;
                if (rpt.MachineName != null)
                {
                    dr["MachineName"] = rpt.MachineName;
                }
                dr["Success"] = rpt.Success;
                dr["Executed"] = rpt.Executed;
                dr["Time"] = rpt.Time;
                if (rpt.Message != null)
                {
                    dr["Message"] = rpt.Message; // == null ? DBNull.Value : rpt.Message;
                }
                if (rpt.StackTrace != null)
                {
                    dr["StackTrace"] = rpt.StackTrace; // == null ? DBNull.Value : (object)rpt.StackTrace;
                }
                dr["TestcaseId"] = rpt.TestcaseId;
                dt_reports.Rows.Add(dr);
            }
            string tempTableName = $"TempReportTable_{DateTime.UtcNow.Ticks}";
            string fields = @"(
    [ReportId] [int] NULL,
    [BuildInstanceId] [int] NOT NULL,
    [MachineName] [varchar](150) NULL,
    [Success] [bit] NOT NULL,
    [Executed] [bit] NOT NULL,
    [Time] [int] NOT NULL,
    [Message] [varchar](max) NULL,
    [StackTrace] [varchar](max) NULL,
    [TestcaseId] [int] NOT NULL
)";
            string queryInsert =
    $@"
INSERT INTO [PEUTEST002].[dbo].[TestReport] 
(
     BuildInstanceId,
     MachineName,
     Success,
     Executed,
     Time,
     Message,
     StackTrace,
     TestcaseId
)
(
SELECT DISTINCT       
     B.BuildInstanceId,
     B.MachineName,
     B.Success,
     B.Executed,
     B.Time,
     B.Message,
     B.StackTrace,
     B.TestcaseId
FROM  [PEUTEST002].[dbo].[TestReport] A
RIGHT JOIN [PEUTEST002].[dbo].[{tempTableName}] B
ON 
    A.BuildInstanceId	     = B.BuildInstanceId
AND A.TestcaseId	 = B.TestcaseId
WHERE A.ReportId IS NULL AND B.TestcaseId <> 0
)";

            string queryUpdate =
    $@"--noop";

            RunSqlQueries(dt_reports, tempTableName, fields, queryInsert, queryUpdate);
        }

        private static void UpdateTableTestcase(PEUTEST002Entities dbc)
        {
            DataTable dt_testcase = new DataTable();
            dt_testcase.Columns.Add(new DataColumn("FixtureId", typeof(int)));
            dt_testcase.Columns.Add(new DataColumn("TestcaseName", typeof(string)));
            dt_testcase.Columns.Add(new DataColumn("TestcaseFullName", typeof(string)));
            

            foreach (LocalTestcase testcase in LocalCollection.AllLocalTestcases)
            {
                if (testcase.TestcaseName.Length <= 800 && testcase.TestcaseFullName.Length <= 1100 && testcase.FixtureId != 0)
                {
                    DataRow dr = dt_testcase.NewRow();
                    dr["FixtureId"] = testcase.FixtureId;
                    dr["TestcaseName"] = testcase.TestcaseName;
                    dr["TestcaseFullName"] = testcase.TestcaseFullName;
                    dt_testcase.Rows.Add(dr);
                }
            }
            string tempTableName = $"##TempTestcaseTable_{DateTime.UtcNow.Ticks}";
            string fields = @"(
    [TestcaseId] [int]  NULL,
    [FixtureId] [int] NOT NULL,
    [TestcaseName] [varchar](800) NOT NULL,
    [TestcaseFullName] [varchar](1100) NULL
)";

            string queryInsert =
                $@"
INSERT INTO [PEUTEST002].[dbo].[Testcase] 
(
TestcaseName, 
TestcaseFullName,
FixtureId
)
(
SELECT DISTINCT  B.TestcaseName, B.TestcaseFullName, B.FixtureId
FROM  [PEUTEST002].[dbo].[Testcase] A
RIGHT JOIN [PEUTEST002].[dbo].[{tempTableName}] B
ON 
    A.FixtureId	     = B.FixtureId
AND A.TestcaseFullName	 = B.TestcaseFullName
AND A.TestcaseName		 = B.TestcaseName
WHERE A.TestcaseId IS NULL
)";
            string queryUpdate =
                $@"--noop";
            RunSqlQueries(dt_testcase, tempTableName, fields, queryInsert, queryUpdate);

            UpdateIds(dbc);
        }

        private static void UpdateTableFixture(PEUTEST002Entities dbc)
        {
            DataTable dt_fixture = new DataTable();
            dt_fixture.Columns.Add(new DataColumn("FixtureName", typeof(string)));
            dt_fixture.Columns.Add(new DataColumn("FixtureFullName", typeof(string)));
            dt_fixture.Columns.Add(new DataColumn("AssemblyId", typeof(int)));

            foreach (LocalFixture fixture in LocalCollection.AllLocalFixtures)
            {
                if (fixture.FixtureName.Length <= 800 && fixture.FixtureFullName.Length <= 1000 && fixture.AssemblyId!=0)
                {
                    DataRow dr = dt_fixture.NewRow();
                    dr["FixtureName"] = fixture.FixtureName;
                    dr["FixtureFullName"] = fixture.FixtureFullName;
                    dr["AssemblyId"] = fixture.AssemblyId;
                    dt_fixture.Rows.Add(dr);
                }
            }
            string tempTableName = $"##TempFixtureTable_{DateTime.UtcNow.Ticks}";
            string fields = @"(
    [FixtureId] [int] NULL,
    [FixtureName] [varchar](800) NOT NULL,
    [FixtureFullName] [varchar](1000) NOT NULL,
    [AssemblyId] [int] NOT NULL
)";

            string queryInsert =
                $@"
INSERT INTO [PEUTEST002].[dbo].[Fixture] 
(
FixtureName, 
FixtureFullName,
AssemblyId
)
(
SELECT DISTINCT  B.FixtureName, B.FixtureFullName, B.AssemblyId
FROM  [PEUTEST002].[dbo].[Fixture] A
RIGHT JOIN [PEUTEST002].[dbo].[{tempTableName}] B
ON 
    A.AssemblyId	     = B.AssemblyId
AND A.FixtureFullName	 = B.FixtureFullName
AND A.FixtureName		 = B.FixtureName
WHERE A.FixtureId IS NULL
)";
            string queryUpdate =
                $@"--noop";
            RunSqlQueries(dt_fixture, tempTableName, fields, queryInsert, queryUpdate);

            UpdateIds(dbc);
        }

        private static void UpdateTableAssembly(PEUTEST002Entities dbc)
        {
            DataTable dt_assembly = new DataTable();
            dt_assembly.Columns.Add(new DataColumn("AssemblyName", typeof(string)));
            dt_assembly.Columns.Add(new DataColumn("AssemblyRelativePath", typeof(string)));
            foreach (LocalAssembly assembly in LocalCollection.LocalAssemblies)
            {
                if (assembly.AssemblyName.Length <= 100 && assembly.AssemblyRelativePath.Length <= 100)
                {
                    DataRow dr = dt_assembly.NewRow();
                    dr["AssemblyName"] = assembly.AssemblyName;
                    dr["AssemblyRelativePath"] = assembly.AssemblyRelativePath;
                    dt_assembly.Rows.Add(dr);
                }
            }


            string tempTableName = $"##TempAssembliesTable_{DateTime.UtcNow.Ticks}";
            string fields = @"(
    [AssemblyName] [varchar](100) NOT NULL,
    [AssemblyRelativePath] [varchar](100) NOT NULL,
    [AssemblyId] [int] NULL
)";

            string queryInsert =
                $@"
INSERT INTO [PEUTEST002].[dbo].[Assembly] 
(
AssemblyName, 
AssemblyRelativePath
)
(
SELECT DISTINCT B.AssemblyName, B.AssemblyRelativePath
FROM  [PEUTEST002].[dbo].[Assembly] A
RIGHT JOIN [PEUTEST002].[dbo].[{tempTableName}] B
ON 
    A.AssemblyName	 = B.AssemblyName
AND A.AssemblyRelativePath		 = B.AssemblyRelativePath
WHERE A.AssemblyId IS NULL
)";
            string queryUpdate =
                $@"UPDATE    [PEUTEST002].[dbo].[Assembly] 
SET
AssemblyName         = B.AssemblyName, 
AssemblyRelativePath = B.AssemblyRelativePath
FROM         [PEUTEST002].[dbo].[Assembly]  A
INNER JOIN   [PEUTEST002].[dbo].[{tempTableName}] B
ON A.AssemblyName = B.AssemblyName 
AND A.AssemblyRelativePath = B.AssemblyRelativePath";
            RunSqlQueries(dt_assembly, tempTableName, fields, queryInsert, queryUpdate);

            UpdateIds(dbc);

        }

        private static void RunSqlQueries(DataTable table, string tempTableName, string fields, string queryInsert, string queryUpdate)
        {
            using (SqlConnection sqlConn = new SqlConnection(ConnectionString))
            {
                sqlConn.Open();


                string createTempQuery = $@"CREATE TABLE {tempTableName}{fields}";
                SqlCommand cmd = new SqlCommand(createTempQuery, sqlConn);
                int retVal = cmd.ExecuteNonQuery();

                using (var transaction = sqlConn.BeginTransaction())
                {
                    var bulkCopy = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.TableLock, transaction);
                    foreach (DataColumn col in table.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }
                    bulkCopy.BulkCopyTimeout = 1000;
                    bulkCopy.DestinationTableName = tempTableName;
                    bulkCopy.WriteToServer(table);
                    transaction.Commit();
                }

                // Insert Into TestReports Table
                SqlCommand cmd1 = new SqlCommand(queryInsert, sqlConn) { CommandTimeout = 1000 };
                int result1 = cmd1.ExecuteNonQuery();
                Console.WriteLine($"# Inserted {result1} records");




                // Update TestReports Table
                SqlCommand cmd2 = new SqlCommand(queryUpdate, sqlConn) { CommandTimeout = 1000 };
                int result2 = cmd2.ExecuteNonQuery();
                Console.WriteLine($"# Updated {result2} records");
            }
        }

        private static void UpdateIds(PEUTEST002Entities dbc)
        {
            var smallList = (from x in dbc.Assemblies
                                           select x).ToList();

            foreach (Assembly assembly in smallList)
            {
                //Console.WriteLine($"{assembly.AssemblyName} / {assembly.AssemblyRelativePath}");
            }
            //foreach (Assembly assembly in smallList)
                foreach (LocalAssembly la in LocalCollection.LocalAssemblies)
            {
                //Console.WriteLine($"Checking {la.AssemblyName} / {la.AssemblyRelativePath}");
                //LocalAssembly laa = new LocalAssembly(assembly.AssemblyName, assembly.AssemblyRelativePath);
                Assembly assembly = (from x in smallList
                                     where x.AssemblyName.Equals(la.AssemblyName) && x.AssemblyRelativePath.Equals(la.AssemblyRelativePath)
                                     select x).ToList().First();
                //LocalAssembly la = (from x in LocalCollection.LocalAssemblies
                //                   where x.Equals(laa)
                //                   select x).ToList().First();
                la.AssemblyId = assembly.AssemblyId;
                foreach (LocalFixture lf in la.LocalFixtures)
                {
                    foreach (Fixture fixx in (from x in assembly.Fixtures
                        where x.FixtureName == lf.FixtureName && x.FixtureFullName == lf.FixtureFullName && lf.AssemblyId == x.AssemblyId
                        select x))
                    {
                        lf.FixtureId = fixx.FixtureId;
                        foreach (LocalTestcase lt in lf.LocalTestcases)
                        {
                            foreach (Testcase tcc in (from x in fixx.Testcases
                                                      where x.TestcaseName == lt.TestcaseName && x.TestcaseFullName == lt.TestcaseFullName && x.FixtureId == lt.FixtureId
                                                      select x))
                            {
                                lt.TestcaseId = tcc.TestcaseId;
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }

        private static void ProcessBuildInstance(string build, PEUTEST002Entities dbc, BuildDefinition buildDefinition)
        {
            try
            {
                var dateStr = GetTestDateString(Directory.GetCreationTime(build));
                TestDate dbDate = GetAddTestDate(dbc, dateStr);
                var buildInstanceName = Path.GetFileName(build);

                var buildDefnId = buildDefinition.BuildDefinitionId;
                BuildInstance buildInstance = GetAddBuildInstance(dbc, buildDefnId, buildInstanceName, dateStr);

                //var resultsDir = Path.Combine(buildDefinition.ResultsShare, buildDefinition.BuildDefinitionName, build, buildDefinition.ResultsPath);
                foreach (string resultsDir in Directory.GetDirectories(build, buildDefinition.ResultsPath, SearchOption.TopDirectoryOnly))
                {
                    foreach (string xmlFile in Directory.GetFiles(resultsDir, "TestResult_*.xml", SearchOption.AllDirectories))
                    {
                        ProcessAssembly(dbc, xmlFile, buildInstance, resultsDir);
                    }
                }
                //var resultsDir = Path.Combine(build, buildDefinition.ResultsPath);
            }
            catch (Exception ex1Exception)
            {
                WriteException(ex1Exception);
                //Environment.Exit(-1);
            }
        }

        private static void ProcessAssembly(PEUTEST002Entities dbc, string xmlFile, BuildInstance buildInstance, string resultsDir)
        {
            try
            {
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(CopyLocal(xmlFile));

                if (!CheckIfValidNUnitXml(xdoc))
                {
                    return;
                }

                Console.WriteLine($"Loaded file {xmlFile} {buildInstance.BuildInstanceName}");

                //var asemblyname = GetAssemblyName(xdoc);
                var vmName = GetVmName(xdoc);
                string assemblyName = GetAssemblyName(xdoc);
                string xmlRelativePath = Path.GetDirectoryName(xmlFile).Replace($"{resultsDir}\\", string.Empty);
                //Assembly assembly = GetAddAssembly(dbc, assemblyName, xmlRelativePath);
                AddAssembly(assemblyName, xmlRelativePath);


                foreach (XmlNode testcaseNode in xdoc.GetElementsByTagName("test-case"))
                {
                    try
                    {
                        XmlNode fixtureNode = GetFixtureNode(testcaseNode);
                        var fixtureName = FormatChars(fixtureNode.Attributes["name"].Value);
                        var fixtureFullName = GetFixtureFullNamespacePath(fixtureNode);

                        var testcaseFullName = FormatChars(testcaseNode.Attributes["name"].Value).Trim('.');
                        var testcaseName = testcaseFullName.Replace($"{fixtureFullName}.", string.Empty);

                        bool executed, success;
                        int time;
                        string message, stacktrace;
                        GetValues(testcaseNode, out executed, out success, out time, out message, out stacktrace);


                        if (testcaseName == testcaseFullName)
                        {
                            testcaseName = GetTestcaseNameFromFullName(testcaseFullName);
                        }
                        if (testcaseName == testcaseFullName)
                        {
                            throw new Exception($"Unable to find testcase correct name: TestcaseName:{testcaseFullName}");
                        }
                        //testcaseFullName.Replace($"{fixtureFullName}.", string.Empty);

                        if (!testcaseFullName.Contains(fixtureFullName))
                        {
                            // Console.WriteLine($"#### Fixture <> Testcasename : Fixturename={fixtureFullName} / Testcasename={testcaseFullName}");
                            //fixtureFullName = testcaseFullName.Substring(0, testcaseFullName.LastIndexOf("."));
                            //throw new Exception($"Unable to find testcase/fixture correct name: Fixname:{fixtureNameFull}, TestcaseName:{testcaseFullName}");
                        }
                        if (!fixtureFullName.Contains(fixtureName))
                        {
                            //fixtureNameFull = testcaseFullName.Substring(0, testcaseFullName.LastIndexOf("."));
                            throw new Exception($"Unable to find fixture correct name: Fixname:{fixtureFullName} / FixtureName:{fixtureName} / TestcaseName:{testcaseFullName}");
                        }
                        //Fixture fixture = GetAddFixture(dbc, fixtureFullName, fixtureName, assembly.AssemblyId);
                        AddFixture(fixtureFullName, fixtureName);
                        try
                        {

                            //var fixtureId = fixture.FixtureId;
                            if (testcaseName == testcaseFullName)
                            {
                                throw new Exception($"Unable to find testcase/fixture correct name: Fixname:{fixtureFullName}, TestcaseName:{testcaseFullName}");
                            }
                            //Testcase testcase = GetAddTestcase(dbc, testcaseName, testcaseFullName, fixtureId);
                            AddTestcase(testcaseName, testcaseFullName);
                            try
                            {
                                AddReport(buildInstance.BuildInstanceId, time, vmName, executed, success, message, stacktrace);
                            }
                            catch (Exception eeee)
                            {
                                WriteException(eeee);
                            }
                        }
                        catch (Exception eee)
                        {
                            WriteException(eee);
                        }
                    }
                    catch (Exception exception)
                    {
                        WriteException(exception);
                        //Environment.Exit(-1);
                    }

                }
                //foreach (XmlNode testcaseNode in xdoc.GetElementsByTagName("test-case"))
                //{
                //    ProcessFixture(dbc, testcaseNode, assembly);
                //}
            }
            catch (Exception ex2Exception)
            {
                WriteException(ex2Exception);
                //Environment.Exit(-1);
            }
        }

        private static void GetValues(XmlNode node, out bool executed, out bool success, out int time, out string message, out string stacktrace)
        {
            executed = success = false;
            time = 0;
            message = stacktrace = null;
            foreach (XmlAttribute attribute in node.Attributes)
            {
                switch (attribute.Name)
                {
                    case "executed":
                        executed = attribute.Value == "True";
                        break;
                    case "success":
                        success = attribute.Value == "True";
                        break;
                    case "time":
                        time = GetIntValue(attribute.Value);
                        break;
                }
            }
            if (executed && !success)
            {
                if (node.HasChildNodes)
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        if (childNode.HasChildNodes)
                        {
                            foreach (XmlNode grandChildNode in childNode.ChildNodes)
                            {
                                if (grandChildNode.Name == "message")
                                {
                                    message = grandChildNode.InnerText;
                                }
                                if (grandChildNode.Name == "stack-trace")
                                {
                                    stacktrace = grandChildNode.InnerText;
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Get the integer value of the cell
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int GetIntValue(string value)
        {
            int integerPart;
            int decimalPart = 0;
            //Split string like "1.932" --> "1" & "932"
            string[] parts = value.Split(".".ToCharArray());
            //convert "1" string to 1 int
            int.TryParse(parts[0], out integerPart);
            //If test has taken abnormally long like > 30 mins (1800 secs)
            /*
             *  if (integerPart > 1800)
             *  {
             *      //for example if integerPart = 12200
             *      // retd = 1.22
             *      double retd = integerPart/1000.0;
             *      // integerPart = 1
             *      integerPart = integerPart/1000;
             *      // decimalPart = (1.22 - 1) x 10 = 22
             *      decimalPart = (int) ((retd - integerPart)*10);
             *  }
             */
            // Check second part of "1.932" --> "932"
            if (parts.Length > 1)
            {
                // split "932" --> char[] {'9','3','2'}
                // convert 1st char - '9' to string "9" --> then to int 9
                Int32.TryParse(parts[1].ToCharArray(0, 1)[0].ToString(), out decimalPart);
            }
            // Round the number to nearest integer. 
            // For ex if 1.890 --> 2
            // 1.2 --> 1,    1.5 --> 2
            // If >= .5 then add 1 
            // Or If integerPart = 0 then add 1 
            if ((integerPart == 0) || (decimalPart >= 5))
            {
                integerPart++;
            }
            return integerPart;
        }
        private static string CopyLocal(string xmlFile)
        {
            string currentDir = Environment.CurrentDirectory;
            var root = Path.GetPathRoot(xmlFile);
            if (!root.EndsWith("\\\\"))
            {
                root = $"{root}\\";
            }
            var newpath = xmlFile.Replace(root, currentDir);
            if (!File.Exists(newpath))
            {
                var dirto = Path.GetDirectoryName(newpath);
                Directory.CreateDirectory(dirto);
                var dirfrom = Path.GetDirectoryName(xmlFile);
                var log = Path.Combine(currentDir, "robocopy.log");
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.Arguments = $@"/c robocopy /LEV:1 /LOG+:""{log}"" ""{dirfrom}"" ""{dirto}"" TestResult_*.xml";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    p.WaitForExit();
                }
            }
            return newpath;
        }

        private static string GetTestcaseNameFromFullName(string teststring)
        {
            string newstrwithTabsForproblemDots = ReplaceNotRequiredDotsWithTabs(teststring);
            return newstrwithTabsForproblemDots.Split(".".ToCharArray()).Last().Replace("\t", ".");
        }

        //enum EndingChar
        //{
        //    RoundBracket,
        //    BoxBracket,
        //    CurlyBracket,
        //    AngularBracket
        //}

        private static string ReplaceNotRequiredDotsWithTabs(string stringTest)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Stack<bool> stack = new Stack<bool>();
            foreach (char x in stringTest)
            {
                char c = x;
                switch (c)
                {
                    case '<':
                    case '[':
                    case '{':
                    case '(':
                        stack.Push(true);
                        break;
                    case '>':
                    case ']':
                    case '}':
                    case ')':
                        if (stack.Count > 0)
                        {
                            stack.Pop();
                        }
                        break;
                    case '.':
                        if (stack.Count > 0)
                        {
                            c = '\t';
                        }
                        break;
                }
                stringBuilder.Append(c);
            }
            var newstring = stringBuilder.ToString();
            return ReplaceUrlDotsWithTabs(newstring);
        }

        private static string ReplaceUrlDotsWithTabs(string text)
        {
            var regexstr = @"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)";
            var matches = Regex.Matches(text, regexstr);
            foreach (Match match in matches)
            {
                var piece = text.Substring(match.Index, match.Length);
                var piecewodot = piece.Replace(".", "\t");
                text = text.Replace(piece, piecewodot);
            }
            return text;
        }

        public static void WriteException(Exception e)
        {
            string inner = string.Empty;
            do
            {
                Console.WriteLine($"{inner}Exception thrown: Type: {e.GetType().Name}, Message: {e.Message}");
                Console.WriteLine(e.StackTrace);
                e = e.InnerException;
                inner = string.IsNullOrEmpty(inner) ? "Inner " : inner;
            } while (e != null);
        }

        private static string GetAssemblyName(XmlDocument xdoc)
        {
            return Path.GetFileName(xdoc?.DocumentElement?.Attributes["name"]?.Value);
        }

        private static bool CheckIfValidNUnitXml(XmlDocument xdoc)
        {
            return (xdoc.DocumentElement.Name == "test-results");
        }


        private static BuildInstance GetAddBuildInstance(PEUTEST002Entities dbc, int buildDefnId, string buildInstanceName, string dateStr)
        {
            var countBuildInstances = (from x in DatabaseInstance.BuildInstances
                                       where x.BuildDefinitionId == buildDefnId && x.BuildInstanceName == buildInstanceName
                                       select x).Count();
            if (countBuildInstances <= 0)
            {
                dbc.BuildInstances.Add(new BuildInstance
                {
                    BuildDefinitionId = buildDefnId,
                    BuildInstanceName = buildInstanceName,
                    DateId = dateStr
                });
                dbc.SaveChanges();
                
                BuildInstance bi = (from x in dbc.BuildInstances
                                    where x.BuildDefinitionId == buildDefnId && x.BuildInstanceName == buildInstanceName
                                    select x).ToList()[0];
                Console.WriteLine($"Added Build Instance {bi.BuildDefinition.BuildDefinitionName} / {buildInstanceName}");
                DatabaseInstance.BuildInstances.Add(bi);
            }
            return (from x in DatabaseInstance.BuildInstances
                where x.BuildDefinitionId == buildDefnId && x.BuildInstanceName == buildInstanceName
                select x).ToList()[0];
        }

        private static TestDate GetAddTestDate(PEUTEST002Entities dbc, string TestDateStr)
        {
            var count = (from u in DatabaseInstance.TestDates
                         where u.DateId == TestDateStr
                         select u).Count();
            if (count <= 0)
            {
                dbc.TestDates.Add(new TestDate { DateId = TestDateStr });
                dbc.SaveChanges();
                TestDate td = (from u in dbc.TestDates
                    where u.DateId == TestDateStr
                    select u).ToList()[0];
                DatabaseInstance.TestDates.Add(td);
            }
            return (from u in DatabaseInstance.TestDates
                           where u.DateId == TestDateStr
                           select u).ToList()[0];
        }

        private static void AddAssembly(string assembly, string assemblyRelativePath)
        {
            LocalAssembly la = new LocalAssembly(assembly, assemblyRelativePath);
            if (!LocalCollection.LocalAssemblies.Contains(la) && !la.AssemblyName.Contains(" "))
            {
                LocalCollection.LocalAssemblies.Add(la);
            }
            LocalCollection.CurrentLocalAssembly = (from x in LocalCollection.LocalAssemblies
                where x.Equals(la)
                select x).First();
        }
        private static void AddFixture(string fixtureFullName, string fixtureName)
        {
            LocalFixture lf = new LocalFixture(LocalCollection.CurrentLocalAssembly, fixtureName, fixtureFullName);
            if (!LocalCollection.CurrentLocalAssembly.LocalFixtures.Contains(lf))
            {
                LocalCollection.CurrentLocalAssembly.LocalFixtures.Add(lf);
            }
            LocalCollection.CurrentLocalAssembly.CurrentLocalFixture = (from x in LocalCollection.CurrentLocalAssembly.LocalFixtures
                                                                        where x.Equals(lf)
                                                                        select x).First();
        }
        private static void AddReport(int build,int time, string vmName, bool executed, bool success, string message, string stacktrace)
        {
            LocalReport lr = new LocalReport(LocalCollection.CurrentLocalAssembly.CurrentLocalFixture.CurrentLocalTestcase, build, time, vmName, executed, success, message, stacktrace);
            if (!LocalCollection.CurrentLocalAssembly.CurrentLocalFixture.CurrentLocalTestcase.LocalReports.Contains(lr))
            {
                LocalCollection.CurrentLocalAssembly.CurrentLocalFixture.CurrentLocalTestcase.LocalReports.Add(lr);
            }
        }
        private static void AddTestcase(string name, string testcaseFullName)
        {
            LocalTestcase lt = new LocalTestcase(LocalCollection.CurrentLocalAssembly.CurrentLocalFixture, name, testcaseFullName);
            if (!LocalCollection.CurrentLocalAssembly.CurrentLocalFixture.LocalTestcases.Contains(lt))
            {
                LocalCollection.CurrentLocalAssembly.CurrentLocalFixture.LocalTestcases.Add(lt);
            }
            LocalCollection.CurrentLocalAssembly.CurrentLocalFixture.CurrentLocalTestcase = (from x in LocalCollection.CurrentLocalAssembly.CurrentLocalFixture.LocalTestcases
                                                                                             where x.Equals(lt)
                                                                                             select x).First();
        }
        private static string GetTestDateString(DateTime TestDateTime)
        {
            return TestDateTime.ToString("yyMMdd");
        }
        private static string GetVmName(XmlDocument top)
        {
            var xmlNodeList = top?.GetElementsByTagName("environment");
            if (xmlNodeList != null)
            {
                foreach (XmlNode env in xmlNodeList)
                {
                    return FormatChars(env?.Attributes?["machine-name"]?.Value);
                }
            }
            return string.Empty;
        }

        private static string GetFixtureFullNamespacePath(XmlNode node)
        {
            string namespaceStr = string.Empty;
            if (node == null)
            {
                return null;
            }
            while (true)
            {
                if ((node?.Name == "test-suite") 
                    && (node.Attributes?["type"]?.Value!= "Assembly") 
                    //&& (node.Attributes?["type"]?.Value != "GenericFixture")
                    //&& (node.Attributes?["type"]?.Value != "ParameterizedFixture")
                    )
                {
                    namespaceStr = $"{FormatChars(node.Attributes?["name"]?.Value)}{(string.IsNullOrEmpty(namespaceStr) ? string.Empty : $".{namespaceStr}")}";
                }
                if (node.ParentNode == null)
                {
                    break;
                }
                node = node.ParentNode;
            }
            return namespaceStr;
        }

        private static XmlNode GetFixtureNode(XmlNode node)
        {
            if ((node?.Name == "test-suite") /*&& (node.Attributes?["type"]?.Value == "TestFixture")*/ || (node?.ParentNode == null))
            {
                return node;
            }
            return GetFixtureNode(node.ParentNode);
        }

        /*







        */


        private static string FormatChars(string value)
        {
            value = HttpUtility.HtmlDecode(value);
            value = value.Replace("&quot;", "\"")
.Replace("&amp;", "&")
.Replace("&lt;", "<")
.Replace("&gt;", ">")
.Replace("&OElig;", "Œ")
.Replace("&oelig;", "œ")
.Replace("&Scaron;", "Š")
.Replace("&scaron;", "š")
.Replace("&Yuml;", "Ÿ")
.Replace("&circ;", "^")
.Replace("&tilde;", "~")
.Replace("&ensp;", " ")
.Replace("&emsp;", " ")
.Replace("&thinsp;", "?")
.Replace("&zwnj;", "?")
.Replace("&zwj;", "?")
.Replace("&lrm;", "?")
.Replace("&rlm;", "?")
.Replace("&ndash;", "–")
.Replace("&mdash;", "—")
.Replace("&lsquo;", "‘")
.Replace("&rsquo;", "’")
.Replace("&sbquo;", "‚")
.Replace("&ldquo;", "“")
.Replace("&rdquo;", "”")
.Replace("&bdquo;", "„")
.Replace("&dagger;", "†")
.Replace("&Dagger;", "‡")
.Replace("&permil;", "‰")
.Replace("&lsaquo;", "<")
.Replace("&rsaquo;", ">")
.Replace("&euro;", "€")
.Replace("ı","i");
            return Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(value));
        }

    }
}
