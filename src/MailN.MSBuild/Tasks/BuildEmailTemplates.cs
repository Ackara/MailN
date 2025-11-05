using MailN.Compilation;
using Microsoft.Build.Framework;
using System.IO;

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
            foreach (ITaskItem sourceFile in SourceFiles)
            {
                if (Path.GetFileName(sourceFile.ItemSpec).StartsWith("_")) continue;
                TemplateEngine.Build(DestinationFolder.GetMetadata("FullPath"), sourceFile.GetMetadata("FullPath"));
                WriteInfo($"compiled '{sourceFile.ItemSpec}'");
            }
            return true;
        }

        public void WriteInfo(string message)
        {
            BuildEngine?.LogMessageEvent(new BuildMessageEventArgs(message, null, nameof(BuildEmailTemplates), MessageImportance.Normal));
        }
    }
}