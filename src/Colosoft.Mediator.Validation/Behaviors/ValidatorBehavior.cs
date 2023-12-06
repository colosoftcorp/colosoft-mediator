using FluentValidation;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.Mediator.Behaviors
{
    public class ValidatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<ValidatorBehavior<TRequest, TResponse>> logger;
        private readonly IEnumerable<IValidator<TRequest>> validators;

        public ValidatorBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidatorBehavior<TRequest, TResponse>> logger)
        {
            this.validators = validators;
            this.logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var typeName = request.GetGenericTypeName();

            this.logger.LogInformation("Validating command {CommandType}", typeName);

            var failures = (await Task.WhenAll(this.validators
                .Select(v => v.ValidateAsync(request, cancellationToken))))
                .SelectMany(result => result.Errors)
                .Where(error => error != null)
                .ToList();

            if (failures.Any())
            {
                this.logger.LogWarning("Validation errors - {CommandType} - Command: {@Command} - Errors: {@ValidationErrors}", typeName, request, failures);

                throw new ValidationException(
                    $"Command Validation Errors for type {typeof(TRequest).Name}",
                    failures);
            }

            return await next();
        }
    }
}
