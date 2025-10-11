using CampaignWatchWorker.Domain.Models.Interfaces;
using FluentValidation;

namespace CampaignWatchWorker.Infra.MultiTenant
{
    public class Tenant : ITenant
    {
        public Tenant(string id, string name, string database, string generateLeadReport, string queueNameProcessorLeads, string queueNameProcessorLeadsUnitary, string queueNameProcessorLeadsShort, string sftpHost, string sftpPassword, string sftpUsername, string sftpDirectory)
        {
            Id = id;
            Name = name;
            Database = database;
            GenerateLeadReport = generateLeadReport;
            QueueNameProcessorLeads = queueNameProcessorLeads;
            QueueNameProcessorLeadsUnitary = queueNameProcessorLeadsUnitary;
            QueueNameProcessorLeadsShort = queueNameProcessorLeadsShort;
            SftpHost = sftpHost;
            SftpPassword = sftpPassword;
            SftpUsername = sftpUsername;
            SftpDirectory = sftpDirectory;

            Validate();
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Database { get; private set; }
        public string GenerateLeadReport { get; private set; }
        public string QueueNameProcessorLeads { get; private set; }
        public string QueueNameProcessorLeadsUnitary { get; private set; }
        public string QueueNameProcessorLeadsShort { get; private set; }
        public string SftpHost { get; private set; }

        public string SftpPassword { get; private set; }

        public string SftpUsername { get; private set; }

        public string SftpDirectory { get; private set; }

        public void Validate()
        {
            var validateResult = new TenantMessageValidation().Validate(this);

            if (!validateResult.IsValid)
                throw new ArgumentException(String.Join(" e ", validateResult.Errors.Select(x => x.ErrorMessage)));
        }

        public class TenantMessageValidation : AbstractValidator<Tenant>
        {
            public TenantMessageValidation()
            {
                RuleFor(c => c.Id)
                    .Must(IsNullOrEmpty)
                    .WithMessage($"{nameof(Id)} não é um ObjectId válido");

                RuleFor(c => c.Database)
                    .Must(IsNullOrEmpty)
                    .WithMessage($"{nameof(Database)} não pode ser nulo ou vazio");

                RuleFor(c => c.QueueNameProcessorLeads)
                    .Must(IsNullOrEmpty)
                    .WithMessage($"{nameof(QueueNameProcessorLeads)} não pode ser nulo ou vazio");

                RuleFor(c => c.QueueNameProcessorLeadsUnitary)
                    .Must(IsNullOrEmpty)
                    .WithMessage($"{nameof(QueueNameProcessorLeadsUnitary)} não pode ser nulo ou vazio");

                RuleFor(c => c.QueueNameProcessorLeadsShort)
                    .Must(IsNullOrEmpty)
                    .WithMessage($"{nameof(QueueNameProcessorLeadsShort)} não pode ser nulo ou vazio");
            }

            protected static bool IsNullOrEmpty(string? value)
                => !string.IsNullOrEmpty(value);
        }
    }
}
