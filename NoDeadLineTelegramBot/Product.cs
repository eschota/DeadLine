using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public class Product
{
    

    public int product_id = -1;
    public string product_name= "";
    public string product_url;
    public string product_url_preview= ""; 
    public List<ParseData> parse_data = new List<ParseData>();
    public string author_name= "";
    public DateTime submit_date = new DateTime(1, 1, 1);
    public List<string> formats = new List<string>();

    public enum cert { not_parsed = -1, no = 0, lite = 1, pro = 2, steam_cell = 3 };
    public cert certificate = cert.not_parsed;
    public string author_url = "";
    public string description = "";
    public List<string> tags = new List<string>();
    public DateTime last_product_page_update;
    public DateTime next_product_page_update;
    public int try_to_parse_counter;
    public class ParseData
    {
        public DateTime date;
        public int pos;
        public float price;
        public ParseData(DateTime _date, int _pos, float _price)
        {
            date = _date;
            pos = _pos;
            price = _price;
        }
    }
    public bool checkNeedToParse
    {
        get
        {
            var now = DateTime.Now;

            if (last_product_page_update == DateTime.MinValue || submit_date == DateTime.MinValue)
            {
                // Если даты не инициализированы, считаем, что нужно парсить
                return true;
            }

            var daysSinceLastUpdate = (now - last_product_page_update).TotalDays;            

            return daysSinceLastUpdate > 30 ;
        }
    }
    public string html_file_path_directory ;
   
   
    public class CustomDateTimeConverter : IsoDateTimeConverter
    {
        public CustomDateTimeConverter()
        {
            base.DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffK";
        }
    }

    
   
}