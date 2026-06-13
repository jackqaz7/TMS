using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace CoreAPI.Filters
{
	public class MethodFilter : ActionFilterAttribute
	{
		private readonly string _AllowedMethod;

		public MethodFilter(string allowedMethod)
		{
			_AllowedMethod = allowedMethod;
		}
        public override void OnActionExecuting(ActionExecutingContext context)
        {
			var SentMethod = context.HttpContext.Request.Method;

            if (SentMethod != _AllowedMethod)
			{
				context.Result = new BadRequestObjectResult(new { error = $"Please use {_AllowedMethod} method" });

			}
        }
	}
}
