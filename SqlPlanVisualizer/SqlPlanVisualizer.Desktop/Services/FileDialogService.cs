namespace SqlPlanVisualizer.Desktop.Services
{
    internal class FileDialogService
    {
        // A kiválasztott SQL-fájl megnyitása és beolvasása
        public string? SelectSqlFile()
        {

            var fileBrowser = new Microsoft.Win32.OpenFileDialog();
            fileBrowser.Title = "SQL-fájl kiválasztása";
            fileBrowser.Filter = "SQL-fájlok (*.sql)|*.sql";
            fileBrowser.Multiselect = false;
            fileBrowser.CheckFileExists = true;

            if (fileBrowser.ShowDialog() == true)
            {
                return fileBrowser.FileName;
            }
            return null;
        }

        // A kiválasztott JSON-fájl megnyitása és beolvasása
        public string? SelectJsonFile()
        {
            var fileBrowser = new Microsoft.Win32.OpenFileDialog();
            fileBrowser.Title = "EXPLAIN JSON-fájl kiválasztása";
            fileBrowser.Filter = "JSON-fájlok (*.json)|*.json";
            fileBrowser.Multiselect = false;
            fileBrowser.CheckFileExists = true;
            if (fileBrowser.ShowDialog() == true)
            {
                return fileBrowser.FileName;
            }
            return null;
        }
    }
}
