using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Elympics
{
	internal class ProjectVersionPostprocessing : AssetPostprocessor
	{
		[Serializable]
		private struct SimplifiedPackageInfo
		{
			public string version;
			public string name;
		}

		private const string AssemblyInfoDirectory = @"Runtime\AssemblyInfo.cs";
		private const string PackageConfigFileName = "package.json";
		private const string ElympicsPackageName = "com.daftmobile.elympics";

		private void OnPreprocessAsset()
		{
			var fileName = Path.GetFileName(assetPath);
			if (fileName.Equals(PackageConfigFileName))
			{
				try
				{
					string versionFromPackage = string.Empty;
					string assemblyInfoPath = string.Empty;
					using (StreamReader reader = new StreamReader(assetPath))
					{
						var json = reader.ReadToEnd();
						var versionData = JsonUtility.FromJson<SimplifiedPackageInfo>(json);

						if (!versionData.name.Equals(ElympicsPackageName))
							return;

						versionFromPackage = versionData.version;
						DirectoryInfo directoryInfo = new DirectoryInfo(assetPath);
						var dInfo = directoryInfo.Parent;
						assemblyInfoPath = Path.Combine(dInfo.FullName, AssemblyInfoDirectory);
					}

					using (StreamWriter writer = new StreamWriter(assemblyInfoPath))
					{
						writer.Write(CombineVersionWithAssemblyData(versionFromPackage));
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		private string CombineVersionWithAssemblyData(string version)
		{
			var split = version.Split('.');
			return $@"//File generated via ProjectVersionPostprocessing.cs. Last date of update {DateTime.UtcNow}
				using System.Reflection;
				using System.Runtime.CompilerServices;
				[assembly: InternalsVisibleTo(""Elympics.Tests"")]
				[assembly: InternalsVisibleTo(""Elympics.Editor"")]
				[assembly: AssemblyVersion(""{split[0]}.{split[1]}.{split[2]}.0"")]";
		}
	}
}

