namespace PeopleRise.SharedKernel;

// CQRS-lite handler contracts shared by every module. No MediatR: handlers are plain DI-resolved
// classes. Splitting command vs query keeps the read/write intent visible and lets the scanner
// (and any future pipeline) treat them differently.

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken ct);
}
