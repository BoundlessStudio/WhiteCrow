using System.Text.Json;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel; // For XLS

namespace WhiteCrow.Services
{
  public class ExcelxlsService :  IChunkify
  {
    public async Task<int> ChunkifyAsync(Stream input, Stream output, CancellationToken ct)
    {
      // Depending on whether your file is .xls or .xlsx, choose HSSFWorkbook or XSSFWorkbook
      // For example, if you know it's .xlsx, you can do:
      IWorkbook workbook = new HSSFWorkbook(input);

      var rowsAsStrings = new List<string>();

      // Iterate over all sheets in the workbook
      for (int i = 0; i < workbook.NumberOfSheets; i++)
      {
        ct.ThrowIfCancellationRequested();

        ISheet sheet = workbook.GetSheetAt(i);
        if (sheet == null) continue;

        // Iterate over all rows in this sheet
        for (int rowIndex = sheet.FirstRowNum; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
          ct.ThrowIfCancellationRequested();

          IRow row = sheet.GetRow(rowIndex);
          if (row == null) continue;

          var rowValues = new List<string>();

          // Some rows may have different numbers of cells
          for (int cellIndex = row.FirstCellNum; cellIndex < row.LastCellNum; cellIndex++)
          {
            ct.ThrowIfCancellationRequested();

            ICell cell = row.GetCell(cellIndex);
            if (cell == null)
            {
              rowValues.Add(string.Empty);
              continue;
            }

            // Convert cell to string
            // NPOI automatically converts different cell types to a string with cell.ToString()
            rowValues.Add(cell.ToString());
          }

          // Join all cell values in the row with commas
          string rowString = string.Join(",", rowValues);
          rowsAsStrings.Add(rowString);
        }
      }

      await output.WriteAsync(BinaryData.FromObjectAsJson(rowsAsStrings));
      output.Seek(0, SeekOrigin.Begin);

      return rowsAsStrings.Count;
    }
  }
}
