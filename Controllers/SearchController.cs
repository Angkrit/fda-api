using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FdaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FdaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private static Dictionary<string, FdaData> FdaList = new Dictionary<string, FdaData>();

        // GET api/values
        [HttpGet("{code}")]
        public async Task<ActionResult<FdaData>> GetAsync(string code)
        {
            try
            {
                Regex rgx = new Regex("[^a-zA-Z0-9 -/]");
                code = rgx.Replace(code, "");
                code = code.Trim();
                var fda = FdaList.FirstOrDefault(f => f.Key == code);
                if (fda.Key != null)
                {
                    return fda.Value;
                }

                var client = new HttpClient();
                var values = new Dictionary<string, string>
                {
                    { "number_src", code }
                };
                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync("https://oryor.com/oryor2015/ajax-check-product.php", content);
                var responseString = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<JObject>(responseString);
                var outputs = data?["output"];
                var output = outputs.FirstOrDefault();
                var status = HtmlToPlainText(output?["cncnm"]?.ToString() ?? "");
                var name = "ไม่พบข้อมูล";
                var nameTh = output?["productha"]?.ToString();
                var nameEn = output?["produceng"]?.ToString();
                if (!string.IsNullOrWhiteSpace(nameTh))
                {
                    name = nameTh;
                }
                else if (!string.IsNullOrWhiteSpace(nameEn))
                {
                    name = nameEn;
                }
                var fdaData = new FdaData
                {
                    No = output?["lcnno"]?.ToString() ?? code,
                    Name = name,
                    Status = status
                };
                FdaList.Add(code, fdaData);
                return fdaData;
            }
            catch (Exception e)
            {
                return new FdaData
                {
                    No = code,
                    Name = "ไม่พบข้อมูล",
                    Status = "-"
                };
            }
        }

        // // POST api/values
        [HttpPost]
        public Task<ActionResult<FdaData>> Post([FromForm] string code)
        {
            return GetAsync(code);
        }


        // GET api/values/5
        // [HttpGet("{id}")]
        // public ActionResult<string> Get(int id)
        // {
        //     return "value";
        // }

        // // PUT api/values/5
        // [HttpPut("{id}")]
        // public void Put(int id, [FromBody] string value)
        // {
        // }

        // // DELETE api/values/5
        // [HttpDelete("{id}")]
        // public void Delete(int id)
        // {
        // }

        private string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }
    }
}
