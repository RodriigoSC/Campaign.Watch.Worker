using CampaignWatchWorker.Domain.Models.Interfaces;
using FluentValidation;

namespace CampaignWatchWorker.Infra.MultiTenant
{
    public class Tenant : ITenant
    {
        public Tenant(
            string id,
            string name,
            string databaseCampaign,
            string databaseEffmail,
            string databaseEffsms,
            string databaseEffpush,
            string databaseEffwhatsapp,
            string queueNameMonitoring)
        {
            Id = id;
            Name = name;
            DatabaseCampaign = databaseCampaign;
            DatabaseEffmail = databaseEffmail;
            DatabaseEffsms = databaseEffsms;
            DatabaseEffpush = databaseEffpush;
            DatabaseEffwhatsapp = databaseEffwhatsapp;
            QueueNameMonitoring = queueNameMonitoring;

            Validate();
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string DatabaseCampaign { get; private set; }
        public string DatabaseEffmail { get; private set; }
        public string DatabaseEffsms { get; private set; }
        public string DatabaseEffpush { get; private set; }
        public string DatabaseEffwhatsapp { get; private set; }
        public string QueueNameMonitoring { get; private set; }

        private void Validate()
        {
            var validateResult = new TenantValidation().Validate(this);

            if (!validateResult.IsValid)
                throw new ArgumentException(string.Join(" e ", validateResult.Errors.Select(x => x.ErrorMessage)));
        }

        private class TenantValidation : AbstractValidator<Tenant>
        {
            public TenantValidation()
            {
                RuleFor(c => c.Id)
                    .NotEmpty()
                    .WithMessage($"{nameof(Id)} não pode ser nulo ou vazio");

                RuleFor(c => c.Name)
                    .NotEmpty()
                    .WithMessage($"{nameof(Name)} não pode ser nulo ou vazio");

                RuleFor(c => c.DatabaseCampaign)
                    .NotEmpty()
                    .WithMessage($"{nameof(DatabaseCampaign)} não pode ser nulo ou vazio");

                RuleFor(c => c.QueueNameMonitoring)
                    .NotEmpty()
                    .WithMessage($"{nameof(QueueNameMonitoring)} não pode ser nulo ou vazio");
            }
        }
    }
}
