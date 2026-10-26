namespace SqlPlanVisualizer.Infrastructure.FileSystem
{
    /// <summary>
    /// Szöveges fájlok tartalmának aszinkron beolvasását végzi.
    /// </summary>
    public class TextFileReader
    {
        /// <summary>
        /// Aszinkron módon beolvassa a megadott fájl teljes szövegét.
        /// </summary>
        /// <param name="path">A beolvasandó fájl elérési útja.</param>
        /// <returns>A fájl teljes szövegét tartalmazó feladat.</returns>
        /// <exception cref="ArgumentException">Az útvonal null, üres vagy csak szóközöket tartalmaz.</exception>
        public Task<string> ReadAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A fájl útvonala nem lehet üres.", nameof(path));

            return File.ReadAllTextAsync(path);
        }
    }
}