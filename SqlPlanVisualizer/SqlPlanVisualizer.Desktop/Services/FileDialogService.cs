namespace SqlPlanVisualizer.Desktop.Services
{
    internal class FileDialogService
    {
        /// <summary>
        /// Fájlválasztó ablakot nyit egy SQL-fájl kiválasztásához.
        /// </summary>
        /// <returns>A kiválasztott fájl útvonala, vagy null megszakítás esetén.</returns>
        public string? SelectSqlFile()
        {
            return SelectFile("SQL-fájl kiválasztása", "SQL-fájlok (*.sql)|*.sql");
        }

        /// <summary>
        /// Fájlválasztó ablakot nyit egy EXPLAIN JSON-fájl kiválasztásához.
        /// </summary>
        /// <returns>A kiválasztott fájl útvonala, vagy null megszakítás esetén.</returns>
        public string? SelectJsonFile()
        {
            return SelectFile("EXPLAIN JSON-fájl kiválasztása", "JSON-fájlok (*.json)|*.json");
        }

        /// <summary>
        /// Megnyitja a fájlválasztót a megadott címmel és fájlszűrővel.
        /// Egyszerre egy létező fájl választható ki.
        /// </summary>
        /// <param name="title">Az ablak címe.</param>
        /// <param name="filter">A megjeleníthető fájltípusok szűrője.</param>
        /// <returns>A kiválasztott fájl útvonala, vagy null megszakítás esetén.</returns>
        private static string? SelectFile(string title, string filter)
        {
            var fileBrowser = new Microsoft.Win32.OpenFileDialog
            {
                Title = title,
                Filter = filter,
                Multiselect = false,
                CheckFileExists = true
            };

            return fileBrowser.ShowDialog() == true ? fileBrowser.FileName : null;
        }
    }
}
