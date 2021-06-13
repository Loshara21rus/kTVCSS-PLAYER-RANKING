using MySqlConnector;
using System;
using System.Drawing;
using System.Drawing.Imaging;


namespace WorkNode
{
    public static class Bracket
    {
        public static void UpdateDbBracket(int pos, string text)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"UPDATE cups_bracket SET TEXT = '{text}' WHERE POSITION = {pos};", connection);
            query.ExecuteNonQuery();
            connection.Close();
        }

        // linux path WARN SUKA
        public static void Draw()
        {
            var image = Image.FromFile(@"Images\Cups\bracket.jpg");
            var graphics = Graphics.FromImage(image);

            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"SELECT TEXT, X, Y FROM cups_bracket WHERE TEXT != '' AND POSITION != 0;", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                graphics.DrawString(reader[0].ToString().ToUpper(), new Font("AGENCYR", 12), Brushes.White, int.Parse(reader[1].ToString()), int.Parse(reader[2].ToString()));
            }

            query = new MySqlCommand($"SELECT TEXT, X, Y FROM cups_bracket WHERE POSITION = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                graphics.DrawString(reader[0].ToString().ToUpper(), new Font("Franklin Gothic Medium", 36), Brushes.White, int.Parse(reader[1].ToString()), int.Parse(reader[2].ToString()));
            }

            connection.Close();

            image.Save(@"Images\Cups\bracket_work.png", ImageFormat.Png);
        }
    }
}
