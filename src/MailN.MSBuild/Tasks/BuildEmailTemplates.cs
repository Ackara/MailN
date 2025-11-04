using MailN.Compilation;
using Microsoft.Build.Framework;
using System.IO;
using System.Linq;

namespace MailN.MSBuild.Tasks
{
	public class BuildEmailTemplates : ITask
	{
		[Required]
		public ITaskItem[] SourceFiles { get; set; }

		[Required]
		public ITaskItem DestinationFolder { get; set; }

		public ITaskHost HostObject { get; set; }

		public IBuildEngine BuildEngine { get; set; }

		public bool Execute()
		{
			foreach (string sourceFile in SourceFiles.Select(x => x.GetMetadata("FullPath")))
			{
				if (Path.GetFileName(sourceFile).StartsWith("_")) continue;
				string[] files = TemplateEngine.Build(DestinationFolder.GetMetadata("FullPath"), sourceFile);
				foreach (string resultFiles in files) WriteInfo($"Generated: {resultFiles}");
			}
			return true;
		}

		public void WriteInfo(string message)
		{
			BuildEngine?.LogMessageEvent(new BuildMessageEventArgs(message, null, nameof(BuildEmailTemplates), MessageImportance.Normal));
		}
	}
}