using DotNet.Globbing;

using HtmlAgilityPack;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MailN.Compilation
{
	public static class TemplateEngine
	{
		public static string[] Build(EmailTemplateOptions options, params string[] sourceFiles)
		{
			if (!Directory.Exists(options.OutputFolder)) Directory.CreateDirectory(options.OutputFolder);
			var resultFiles = new string[sourceFiles.Length];

			for (int i = 0; i < sourceFiles.Length; i++)
			{
				string sourceFilePath = sourceFiles[i];
				HtmlNode document = MergeLayouts(sourceFilePath, CreateHtlmNodeFrom(sourceFilePath));
				document = MergeExternalFiles(sourceFilePath, document);
				string resultPath = resultFiles[i] = Path.Combine(options.OutputFolder, Path.GetFileName(sourceFilePath));

				if (File.Exists(resultPath)) File.Delete(resultPath);
				using (var file = new FileStream(resultPath, FileMode.Create, FileAccess.Write, FileShare.Read))
				using (var writer = new StreamWriter(file))
				{
					document.WriteTo(writer);
					writer.Flush();
				}
			}

			return resultFiles;
		}

		public static string[] Build(string outputFolder, params string[] sourceFiles)
			=> Build(new EmailTemplateOptions { OutputFolder = outputFolder }, sourceFiles);

		public static IEnumerable<string> GetContentFiles(string sourceFolder)
		{
			if (string.IsNullOrWhiteSpace(sourceFolder)) return [];
			else if (!Directory.Exists(sourceFolder)) return [];

			return Directory.EnumerateFiles(sourceFolder, "*.html", SearchOption.AllDirectories).Where(x => !Path.GetFileName(x).StartsWith("_"));
		}

		private static IEnumerable<string> FindFiles(string folderPath, string pattern)
		{
			var glob = Glob.Parse(pattern);
			var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

			return [Path.Combine(folderPath, pattern)];
		}

		private static HtmlNode MergeLayouts(string sourceFile, HtmlNode content)
		{
			var matches = content.SelectNodes("//meta[@resource]");
			if (matches == null) return content;
			if (matches.Count >= 2) throw new FormatException($"The '{sourceFile}' file cannot have multiple <meta> tags");

			var metaTag = matches.First();
			string directory = Path.GetDirectoryName(sourceFile);
			var pattern = metaTag.GetAttributeValue<string>("resource", string.Empty);
			string layoutFilePath = FindFiles(directory, pattern).FirstOrDefault();
			if (string.IsNullOrEmpty(layoutFilePath)) throw new FileNotFoundException($"Could not find file that match the '{pattern}' in the '{directory}' directory.");
			content.RemoveChild(metaTag);

			content = merge(CreateHtlmNodeFrom(layoutFilePath), content);
			return MergeLayouts(layoutFilePath, content);

			static HtmlNode merge(HtmlNode layout, HtmlNode content)
			{
				var sectionElements = layout.SelectNodes("//*[@data-render-section]");
				if (sectionElements != null)
					foreach (HtmlNode container in sectionElements)
					{
						string sectionName = container.GetAttributeValue<string>("data-render-section", "body");
						if (sectionName == "body")
						{
							container.ParentNode.InsertAfter(content, container);
							container.Remove();
						}
						else
						{
							var sectionNode = content.SelectSingleNode($"//*[@data-layout-section='{sectionName}']");
							if (sectionNode == null) continue;

							container.ParentNode.InsertAfter(sectionNode, container);
							container.Remove();
							content.RemoveChild(sectionNode);
						}
					}

				return layout;
			}
		}

		private static HtmlNode MergeExternalFiles(string sourceFile, HtmlNode content)
		{
			HtmlNode targetNode;
			foreach ((string xpath, string attributeName) in new (string, string)[] { ("//link", "href"), ("//*[@data-include]", "data-include") })
			{
				while ((targetNode = content.SelectSingleNode(xpath)) != null)
				{
					string pattern = targetNode.GetAttributeValue<string>(attributeName, string.Empty);
					string directory = Path.GetDirectoryName(sourceFile);
					string targetFilePath = FindFiles(directory, pattern).FirstOrDefault();
					if (string.IsNullOrEmpty(targetFilePath)) throw new FileNotFoundException($"Could not find file that match the '{pattern}' in the '{directory}' directory.");

					HtmlNode externalContent;
					using (var file = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
					using (var reader = new StreamReader(file))
					{
						externalContent = attributeName switch
						{
							"href" => HtmlNode.CreateNode(string.Concat("<style>", reader.ReadToEnd(), "</style>")),
							_ => HtmlNode.CreateNode(reader.ReadToEnd())
						};
					}

					targetNode.ParentNode.ReplaceChild(externalContent, targetNode);
				}
			}

			return content;
		}

		private static HtmlNode CreateHtlmNodeFrom(string filePath)
		{
			using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var document = new HtmlDocument();
			document.Load(file);
			return document.DocumentNode;
		}
	}
}