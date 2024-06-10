using Microsoft.VisualBasic.FileIO;

namespace GrpcServer
{
    public class FileHandler
    {
        public static List<string[]> FileReader(string path)
        {
            List<string[]> data = new List<string[]>();

            try
            {
                using (TextFieldParser parser = new TextFieldParser(path))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        data.Add(fields);
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            return data;
        }

        public static void FileWriter(string path, List<string> data)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    foreach (string record in data)
                    {
                        writer.WriteLine(record);
                    }
                }
                Console.WriteLine("Data written to file successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to file: " + ex.Message);
            }
        }
    }
}
