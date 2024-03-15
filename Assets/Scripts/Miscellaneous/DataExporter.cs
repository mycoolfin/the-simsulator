using System.IO;
using System.Linq;
using System.Collections.Generic;

public struct DataColumn
{
    public string header;
    public List<object> data;
}

public static class DataExporter
{
    public static void ExportToCSV(List<DataColumn> data, string filePath)
    {
        using StreamWriter writer = new(filePath, false);

        writer.WriteLine(string.Join(",", data.Select(col => col.header)));

        int maxRows = data.Max(col => col.data.Count);
        for (int i = 0; i < maxRows; i++)
        {
            List<string> rowData = new();
            foreach (DataColumn col in data)
                rowData.Add(i < col.data.Count ? col.data[i].ToString() : "");
            writer.WriteLine(string.Join(",", rowData));
        }

        UnityEngine.Debug.Log("Saved CSV to " + filePath);
    }
}
