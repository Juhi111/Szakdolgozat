namespace SqlPlanVisualizer.Infrastructure.FileSystem
{
    // Fájlolvasó osztály, amely SQL fájlokat olvas be aszinkron módon
    public class SqlFileReader
    {        
        public Task<string> ReadAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A fájl útvonala nem lehet üres", nameof(path));
            
            return File.ReadAllTextAsync(path);            
        }
    }
} 
    

