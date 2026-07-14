using VerticalSliceDemo.Domains;

namespace VerticalSliceDemo.Features
{
    /// <summary>Maps domain exceptions thrown by feature services onto HTTP results.</summary>
    public static class ProblemResults
    {
        public static IResult Map(Exception ex) => ex switch
        {
            ValidationException v => Results.ValidationProblem(v.Errors),
            NotFoundException n => Results.NotFound(n.Message),
            ConflictException c => Results.Conflict(c.Message),
            _ => Results.Problem(detail: ex.Message)
        };
    }
}
