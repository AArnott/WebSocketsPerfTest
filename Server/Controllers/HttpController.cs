using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.AspNetCore.Mvc;
using StreamJsonRpc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HttpController : ControllerBase
    {
        [HttpPost]
        public string Post([FromForm]string character)
        {
            return "HTTP " + character;
        }
    }
}
