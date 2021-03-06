﻿using System.Security.Cryptography;
using EnjoyDialogs.SCIM.Data.Contracts;
using EnjoyDialogs.SCIM.Infrastructure;
using EnjoyDialogs.SCIM.Models;
using Newtonsoft.Json;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;

namespace EnjoyDialogs.SCIM.Controllers
{
    [Authorize]
    [ScimExpceptionHandlerFilter]
    public class UsersController : ApiControllerBase
    {
        public UsersController(IUnitOfWork uow)
        {
            Uow = uow;
        }

        //private readonly IUserService _userService;

        //public UsersController ()
        //    : this(ObjectFactory.GetInstance<IUserService>())
        //{
        //}

        //public UsersController (IUserService userService)
        //{
        //    if (userService == null) throw new ArgumentException();
        //    _userService = userService;
        //}

        // GET v1/Users/
        [HttpGet]
        public IEnumerable<UserModel> Get()
        {

            var result = Uow.Users.GetAll();

            return result;
        }

        // GET v1/Users/5
        [HttpGet]
        [ApiDoc("Gets a user by ID.")]
        [ApiParameterDoc("id", "The ID of the user.", typeof(UserModel))]
        public HttpResponseMessage Get(Guid id)
        {
            var user = Uow.Users.GetById(id);

            if (user == null)
            {
                throw new ScimException(HttpStatusCode.NotFound , string.Format("Resource {0} not found", id));
            }


            IContentNegotiator negotiator = this.Configuration.Services.GetContentNegotiator();
            ContentNegotiationResult result = negotiator.Negotiate(typeof (UserModel), this.Request, this.Configuration.Formatters);
            if (result == null)
            {
                throw new ScimException(HttpStatusCode.NotAcceptable, "Server does not support requested operation");
            }


            return new HttpResponseMessage()
                {
                    Content = new ObjectContent<UserModel>(
                        user, // What we are serializing  
                        result.Formatter, // The media formatter 
                        result.MediaType.MediaType // The MIME type 
                        )
                };
        }

        //POST //v1/Users HTTP/1.1
        //Accept: application/json
        //Content-Type: application/json
        //Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=
        //User-Agent: Jakarta Commons-HttpClient/3.1
        //Host: scim.azurewebsites.net
        //Content-Length: 104
        //{
        //  "id": "",
        //  "schemas": ["urn:scim:schemas:core:1.0"],
        //  "active": true,
        //  "userName": "AliceJson2"
        //}
        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            string jsonString = await ControllerContext.Request.Content.ReadAsStringAsync();


            //TEMPORARY: MOVE TO UserService!
            var jsonObj = JsonConvert.DeserializeObject<UserModel>(jsonString);
            jsonObj.Id = Guid.NewGuid();

            string uri = Url.Link("DefaultApi_v1", new {id = jsonObj.Id});

            var now = DateTime.Now;
            jsonObj.Meta = new MetaModel
                {
                    Created = now,
                    LastModified = now,
                    Location = uri
                    //Version = @"W\/""" + GenerateETag(now) + @""""
                };


            var resultString = JsonConvert.SerializeObject(jsonObj);

            var response = Request.CreateResponse(HttpStatusCode.Created, resultString);
            response.Headers.Location = new Uri(uri);
            return response;
        }

        // PUT v1/Users/5
        [HttpPut]
        public void Put(Guid id, UserModel user) //[FromBody]string value)
        {
            throw new NotImplementedException();
            user.Id = id;
            //if (!repository.Update(user))
            //{
                throw new HttpResponseException(HttpStatusCode.NotFound);
            //} 

        }

        // DELETE v1/Users/5
        [HttpDelete]
        public HttpResponseMessage Delete(Guid id)
        {
            var user = Uow.Users.GetById(id);
            if (user == null)
            {
                throw new ScimException(HttpStatusCode.NotFound, string.Format("Resource {0} not found", id));
            }

            Uow.Users.Delete(user);
            Uow.Commit();

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }


        private string GenerateETag(DateTime lastModified)
        {
            var encoding = new System.Text.UTF8Encoding();
            byte[] checksum = encoding.GetBytes(lastModified.ToUniversalTime().ToString());
            return Convert.ToBase64String(checksum, 0, checksum.Length);
        }

    }
}
