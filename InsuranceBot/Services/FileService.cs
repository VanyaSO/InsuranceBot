namespace InsuranceBot.Services;

public class FileService
{
    public void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}