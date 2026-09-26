namespace SqlPlanVisualizer.Infrastructure.FileSystem
{
    //fájlolvasó osztály, amely JSON fájlokat olvas be aszinkron módon
    public class JsonFileReader
    {
        public Task<string> ReadAsync(string path) { 
            if(string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

            return File.ReadAllTextAsync(path);
        }
    }
}
