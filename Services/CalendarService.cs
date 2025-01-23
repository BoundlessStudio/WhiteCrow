using Ical.Net;


namespace WhiteCrow.Services
{
  public class CalendarService : IChunkify
  {
    public async Task<int> ChunkifyAsync(Stream input, Stream output, CancellationToken ct)
    {
      // Read the ICS file content
      using var reader = new StreamReader(input);
      string icsContent = await reader.ReadToEndAsync();

      // Parse the calendar
      // Note: If your ICS contains multiple calendars, you may need CalendarCollection instead.
      var calendar = Calendar.Load(icsContent);

      // Prepare a list of strings to hold event data
      var eventSummaries = new List<string>();

      // Extract information from each event in the calendar
      foreach (var calEvent in calendar.Events)
      {
        ct.ThrowIfCancellationRequested();

        // Example: Summary, Start and End dates
        var summary = calEvent.Summary;
        var start = calEvent.DtStart?.Value;
        var end = calEvent.DtEnd?.Value;

        // Build a friendly string for each event.
        // Adjust as needed for your use case.
        var eventInfo = $"Event: {summary ?? "No Title"} | Start: {start} | End: {end}";
        eventSummaries.Add(eventInfo);
      }

      // Write the JSON bytes to the output stream
      await output.WriteAsync(BinaryData.FromObjectAsJson(eventSummaries));
      output.Seek(0, SeekOrigin.Begin);

      return eventSummaries.Count;
    }
  }
}
