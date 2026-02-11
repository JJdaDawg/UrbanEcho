using UrbanEcho.FileManagement;

namespace UrbanEcho.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

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