using UrbanEcho.FileManagement;

namespace UrbanEcho.Tests;

/// <summary>
/// Unit tests for Project files. Includes tests to make sure reading
/// the project test file is done correct
/// </summary>
public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    // <summary>
    /// Tests if opening a project file works correctly
    /// </summary>
    [Test]
    public void TestOpenFile()
    {
        ProjectFile? projectFile = null;
        bool failedToOpen = false;

        string fileName = "ProjectTestFile.json";
        try
        {
            projectFile = ProjectFile.Open(fileName);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Project file didn't open with error {ex.ToString()}");
            failedToOpen = true;
        }

        if (projectFile == null)
        {
            Assert.Fail($"Project file null");
            failedToOpen = true;
        }
        if (projectFile != null && failedToOpen != true)
        {
            Assert.Pass($"Opened file {fileName}");
        }
    }

    // <summary>
    /// Tests if saving a project file works correctly
    /// </summary>
    [Test]
    public void TestSaveFile()
    {
        bool failed = false;
        ProjectFile? projectFile = new ProjectFile();

        string fileName = "ProjectTestFile.json";

        projectFile.PathForThisFile = fileName;

        projectFile.BackgroundLayerPath = "";
        projectFile.RoadLayerPath = "";
        projectFile.IntersectionLayerPath = "";

        try
        {
            ProjectFile.Save(projectFile);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Project file didn't save with error {ex.ToString()}");
            failed = true;
        }

        if (!failed)
            Assert.Pass("saved file inspect to see if correct");
    }

    // <summary>
    /// Tests if save as project file works correctly
    /// </summary>
    [Test]
    public void TestSaveAsFile()
    {
        bool failed = false;
        ProjectFile? projectFile = new ProjectFile();

        string fileName = "ProjectTestFile.json";

        projectFile.PathForThisFile = fileName;

        projectFile.BackgroundLayerPath = "";
        projectFile.RoadLayerPath = "";
        projectFile.IntersectionLayerPath = "";

        try
        {
            ProjectFile.SaveAs(projectFile, fileName);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Project file didn't save with error {ex.ToString()}");
            failed = true;
        }

        if (!failed)
            Assert.Pass("saved file inspect to see if correct");
    }
}