// CI only: project_auditor.sh copies this into the testing project's Assets/Editor and calls ProjectAuditorCi.Run with -quit.
// New findings throw, which makes Unity exit with code 1 (EditorApplication.Exit crashed 6.3 batchmode).
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.ProjectAuditor.Editor;
using UnityEditor;
using UnityEngine;

public static class ProjectAuditorCi
{
    /// <summary>
    /// Audits the package at CI_AUDIT_PACKAGE_PATH (e.g. "Packages/com.daftmobile.elympics/") with Roslyn analyzers and compares the
    /// domain-reload and upgrade (obsolete or removed Unity API) findings against the baseline at CI_AUDIT_BASELINE.
    /// Fails on findings that aren't in the baseline. CI_AUDIT_UPDATE_BASELINE=1 rewrites the baseline instead.
    /// Also fails on any compiler error in the project: Project Auditor skips assemblies it can't compile for the platform,
    /// so their findings would silently drop out of the comparison.
    /// Performance hints (LINQ, allocations, string concatenation) are left out on purpose: new code would trip them constantly.
    /// </summary>
    public static void Run()
    {
        var packagePath = Required("CI_AUDIT_PACKAGE_PATH");
        var baselinePath = Required("CI_AUDIT_BASELINE");
        var outputPath = Environment.GetEnvironmentVariable("CI_AUDIT_OUTPUT") ?? "project-auditor-findings.txt";
        var updateBaseline = Environment.GetEnvironmentVariable("CI_AUDIT_UPDATE_BASELINE") == "1";

        var report = Audit();
        var findings = Findings(report, packagePath);
        File.WriteAllLines(outputPath, findings);
        Debug.Log($"ProjectAuditorCi: {findings.Length} domain-reload/upgrade findings in {packagePath}, written to {outputPath}");

        var compilerErrors = CompilerErrors(report);
        Debug.Log($"ProjectAuditorCi: {compilerErrors.Length} compiler errors for {EditorUserBuildSettings.activeBuildTarget}");
        foreach (var error in compilerErrors)
            Debug.LogError("ProjectAuditorCi: compiler error: " + error);
        if (compilerErrors.Length > 0)
            throw new InvalidOperationException($"ProjectAuditorCi: {compilerErrors.Length} compiler error(s); the audit skipped the assemblies that failed to compile.");

        if (updateBaseline)
        {
            File.WriteAllLines(baselinePath, findings);
            Debug.Log($"ProjectAuditorCi: baseline updated at {baselinePath}");
            return;
        }

        var baseline = File.Exists(baselinePath) ? File.ReadAllLines(baselinePath).Where(l => l.Length > 0).ToArray() : Array.Empty<string>();
        var added = findings.Except(baseline).ToArray();
        var resolved = baseline.Except(findings).ToArray();
        foreach (var finding in resolved)
            Debug.Log("ProjectAuditorCi: no longer found (remove from the baseline): " + finding);
        foreach (var finding in added)
            Debug.LogError("ProjectAuditorCi: new finding: " + finding);
        if (added.Length > 0)
            throw new InvalidOperationException($"ProjectAuditorCi: {added.Length} new finding(s) not in {baselinePath}. Fix them, or update the baseline from {outputPath}.");
    }

    private static Report Audit()
    {
        SetUseRoslynAnalyzers(true);
        return new ProjectAuditor().Audit(new AnalysisParams
        {
            Categories = new[] { IssueCategory.Code, IssueCategory.DomainReload, IssueCategory.CodeCompilerMessage },
            Platform = EditorUserBuildSettings.activeBuildTarget,
        });
    }

    /// <returns>Sorted "DescriptorId | path | description" lines, without line numbers so unrelated edits don't churn the baseline.</returns>
    internal static string[] Findings(Report report, string packagePath)
    {
        return report.GetAllIssues()
            .Where(issue => issue.RelativePath != null
                && issue.RelativePath.StartsWith(packagePath, StringComparison.Ordinal)
                && !issue.RelativePath.Contains("/Runtime/Plugins/"))
            .Where(issue => issue.Category == IssueCategory.DomainReload || (issue.Id.IsValid() && (issue.Id.GetDescriptor().Areas & Areas.Upgrade) != 0))
            .Select(issue => $"{issue.Id} | {issue.RelativePath} | {issue.Description}")
            .Distinct()
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();
    }

    /// <returns>"Code | assembly | path:line | message" lines for every error-level compiler message, wherever it is.</returns>
    internal static string[] CompilerErrors(Report report) =>
        report.GetAllIssues()
            .Where(issue => issue.Category == IssueCategory.CodeCompilerMessage && issue.Severity == Severity.Error)
            .Select(issue => $"{issue.GetCustomProperty(CompilerMessageProperty.Code)} | {issue.GetCustomProperty(CompilerMessageProperty.Assembly)} | {issue.RelativePath}:{issue.Line} | {issue.Description}")
            .Distinct()
            .ToArray();

    // Mirrors Project Auditor's internal custom-property layout of compiler messages.
    private enum CompilerMessageProperty
    {
        Code,
        Assembly,
    }

    // UserPreferences is internal; it backs the "Use Roslyn Analyzers" preference that the domain-reload analyzers need.
    private static void SetUseRoslynAnalyzers(bool value)
    {
        var preferences = typeof(ProjectAuditor).Assembly.GetType("Unity.ProjectAuditor.Editor.UserPreferences");
        var property = preferences?.GetProperty("UseRoslynAnalyzers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (property == null)
            throw new InvalidOperationException("Project Auditor's UseRoslynAnalyzers preference was not found.");
        property.SetValue(null, value);
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set.");
}
