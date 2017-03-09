using System.Net.Http;

namespace UpTestDater.Data
{
    public class LocalReport
    {
        public LocalReport(LocalTestcase localTestcase, int buildInstance, int time, string vmName, bool executed = true, bool success = false, string message = null, string stacktrace = null)
        {
            ParentTestcase = localTestcase;
            BuildInstanceId = buildInstance;
            Time = time;
            MachineName = vmName;
            Executed = executed;
            Success = success;
            Message = message;
            StackTrace = stacktrace;
        }

        public LocalTestcase ParentTestcase { get; set; }
        public int BuildInstanceId { get; set; }
        public string MachineName { get; set; }
        public bool Success { get; set; }
        public bool Executed { get; set; }
        public int Time { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public int TestcaseId
        {
            get { return ParentTestcase.TestcaseId; }
        }
        public override bool Equals(object obj)
        {
            LocalReport lr = obj as LocalReport;
            return (lr?.ParentTestcase != null) && lr.ParentTestcase.Equals(ParentTestcase) && (lr.BuildInstanceId == BuildInstanceId);
        }
    }
}