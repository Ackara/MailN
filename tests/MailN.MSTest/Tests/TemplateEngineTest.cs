using ApprovalTests;
using MailN.Compilation;
using MailN.MSTest;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MailN.Tests
{
	[TestClass]
	public class TemplateEngineTest
	{
		[TestMethod]
		[DynamicData(nameof(GetEmailTemplates), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(DisplayTestWithFileName))]
		public void Can_build_html_template(string htmlFilePath)
		{
			// Arrange

			using var approver = ApprovalTests.Namers.ApprovalResults.ForScenario(Path.GetFileNameWithoutExtension(htmlFilePath));

			string resultFolder = Path.Combine(Path.GetTempPath(), nameof(MailN));
			if (!Directory.Exists(resultFolder)) Directory.CreateDirectory(resultFolder);

			var options = new EmailTemplateOptions
			{
				OutputFolder = resultFolder
			};

			// Act

			var resultFiles = TemplateEngine.Build(options, htmlFilePath);

			// Assert

			resultFiles.ShouldNotBeNull();
			resultFiles.ShouldNotBeEmpty();
			Approvals.VerifyFile(resultFiles[0]);
		}

		[TestMethod]
		public void Can_find_content_files_from_directory()
		{
			// Act

			var results = TemplateEngine.GetContentFiles(TestData.Directory);

			// Assert

			results.ShouldNotBeEmpty();
			results.ShouldNotContain(x => Path.GetFileName(x).StartsWith("_"));
		}

		public static IEnumerable<object[]> GetEmailTemplates()
		{
			var htmlFiles = TemplateEngine.GetContentFiles(TestData.Directory);
			foreach (var filePath in htmlFiles) yield return new object[] { filePath };
		}

		public static string DisplayTestWithFileName(MethodInfo methodInfo, object[] values)
		{
			var filePath = Convert.ToString(values[0]);
			return $"{methodInfo.Name} (\"{Path.GetFileName(filePath)}\")";
		}
	}
}