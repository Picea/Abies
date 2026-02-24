using Abies.Validated;
using static Abies.Validated.Extensions;

namespace Abies.Tests;

// =============================================================================
// Validated<T> — Applicative Validation Tests
// =============================================================================
// Covers: Valid/Invalid construction, Map, Bind, Apply (error accumulation),
// multi-arg Apply, LINQ, Match, Traverse, Sequence, Validate extension,
// Filtering, Conversion (Result ↔ Validated), Async, Curry, and Validators.
// =============================================================================

public class ValidatedTests
{
    // -------------------------------------------------------------------------
    // Construction
    // -------------------------------------------------------------------------

    [Fact]
    public void Valid_ContainsValue()
    {
        Validated<int> v = new Valid<int>(42);
        Assert.IsType<Valid<int>>(v);
        Assert.Equal(42, ((Valid<int>)v).Value);
    }

    [Fact]
    public void Invalid_ContainsErrors()
    {
        var error = new ValidationError("field", "oops");
        Validated<int> v = new Invalid<int>(error);
        Assert.IsType<Invalid<int>>(v);
        Assert.Single(((Invalid<int>)v).Errors);
    }

    [Fact]
    public void Invalid_CanHoldMultipleErrors()
    {
        var errors = new[]
        {
            new ValidationError("a", "err1"),
            new ValidationError("b", "err2"),
        };
        Validated<int> v = new Invalid<int>(errors);
        Assert.Equal(2, ((Invalid<int>)v).Errors.Length);
    }

    [Fact]
    public void Factory_Valid_CreatesValidInstance()
    {
        var v = Valid("hello");
        Assert.IsType<Valid<string>>(v);
        Assert.Equal("hello", ((Valid<string>)v).Value);
    }

    [Fact]
    public void Factory_Invalid_CreatesInvalidInstance()
    {
        var v = Invalid<string>("field", "msg");
        Assert.IsType<Invalid<string>>(v);
    }

    // -------------------------------------------------------------------------
    // Map (functor)
    // -------------------------------------------------------------------------

    [Fact]
    public void Map_Valid_TransformsValue()
    {
        var result = Valid("hello").Map(s => s.Length);
        Assert.Equal(5, Assert.IsType<Valid<int>>(result).Value);
    }

    [Fact]
    public void Map_Invalid_PreservesErrors()
    {
        var result = Invalid<string>("f", "e").Map(s => s.Length);
        var invalid = Assert.IsType<Invalid<int>>(result);
        Assert.Single(invalid.Errors);
        Assert.Equal("f", invalid.Errors[0].Field);
    }

    // -------------------------------------------------------------------------
    // Bind (monad — short-circuits)
    // -------------------------------------------------------------------------

    [Fact]
    public void Bind_ValidToValid_ReturnsValid()
    {
        var result = Valid("5").Bind(s =>
            int.TryParse(s, out var n)
                ? Valid(n)
                : Invalid<int>("val", "not a number"));

        Assert.Equal(5, Assert.IsType<Valid<int>>(result).Value);
    }

    [Fact]
    public void Bind_ValidToInvalid_ReturnsInvalid()
    {
        var result = Valid("abc").Bind(s =>
            int.TryParse(s, out var n)
                ? Valid(n)
                : Invalid<int>("val", "not a number"));

        Assert.IsType<Invalid<int>>(result);
    }

    [Fact]
    public void Bind_Invalid_ShortCircuits()
    {
        var called = false;
        var result = Invalid<string>("f", "e").Bind(s =>
        {
            called = true;
            return Valid(s.Length);
        });

        Assert.False(called);
        Assert.IsType<Invalid<int>>(result);
    }

    // -------------------------------------------------------------------------
    // Apply (applicative — accumulates ALL errors) ★ KEY FEATURE ★
    // -------------------------------------------------------------------------

    [Fact]
    public void Apply_BothValid_AppliesFunction()
    {
        var result = Valid<Func<int, int>>(x => x * 2).Apply(Valid(21));
        Assert.Equal(42, Assert.IsType<Valid<int>>(result).Value);
    }

    [Fact]
    public void Apply_FuncInvalid_ArgValid_PropagatesErrors()
    {
        var result = Invalid<Func<int, int>>("f", "bad func").Apply(Valid(1));
        var invalid = Assert.IsType<Invalid<int>>(result);
        Assert.Single(invalid.Errors);
    }

    [Fact]
    public void Apply_FuncValid_ArgInvalid_PropagatesErrors()
    {
        var result = Valid<Func<int, int>>(x => x).Apply(Invalid<int>("a", "bad arg"));
        var invalid = Assert.IsType<Invalid<int>>(result);
        Assert.Single(invalid.Errors);
    }

    [Fact]
    public void Apply_BothInvalid_AccumulatesErrors()
    {
        var result = Invalid<Func<int, int>>("f", "err1")
            .Apply(Invalid<int>("a", "err2"));

        var invalid = Assert.IsType<Invalid<int>>(result);
        Assert.Equal(2, invalid.Errors.Length);
        Assert.Equal("f", invalid.Errors[0].Field);
        Assert.Equal("a", invalid.Errors[1].Field);
    }

    [Fact]
    public void Apply_MultiField_AccumulatesAllErrors()
    {
        // Simulate: Valid((name, email) => new { name, email })
        //   .Apply(invalidName)
        //   .Apply(invalidEmail)

        Validated<Func<string, string, string>> create =
            Valid<Func<string, string, string>>((name, email) => $"{name} <{email}>");

        var result = create
            .Apply(Invalid<string>("name", "name is required"))
            .Apply(Invalid<string>("email", "email is invalid"));

        var invalid = Assert.IsType<Invalid<string>>(result);
        Assert.Equal(2, invalid.Errors.Length);
        Assert.Contains(invalid.Errors, e => e.Field == "name");
        Assert.Contains(invalid.Errors, e => e.Field == "email");
    }

    [Fact]
    public void Apply_MultiField_AllValid_ReturnsResult()
    {
        Validated<Func<string, string, string>> create =
            Valid<Func<string, string, string>>((name, email) => $"{name} <{email}>");

        var result = create
            .Apply(Valid("Alice"))
            .Apply(Valid("alice@example.com"));

        Assert.Equal("Alice <alice@example.com>", Assert.IsType<Valid<string>>(result).Value);
    }

    [Fact]
    public void Apply_ThreeFields_AccumulatesAllErrors()
    {
        Validated<Func<string, string, int, string>> create =
            Valid<Func<string, string, int, string>>((n, e, a) => $"{n},{e},{a}");

        var result = create
            .Apply(Invalid<string>("name", "required"))
            .Apply(Invalid<string>("email", "invalid"))
            .Apply(Invalid<int>("age", "too young"));

        var invalid = Assert.IsType<Invalid<string>>(result);
        Assert.Equal(3, invalid.Errors.Length);
    }

    [Fact]
    public void Apply_FourFields_AccumulatesAllErrors()
    {
        Validated<Func<string, string, int, bool, string>> create =
            Valid<Func<string, string, int, bool, string>>((a, b, c, d) => "ok");

        var result = create
            .Apply(Invalid<string>("a", "e1"))
            .Apply(Invalid<string>("b", "e2"))
            .Apply(Invalid<int>("c", "e3"))
            .Apply(Invalid<bool>("d", "e4"));

        var invalid = Assert.IsType<Invalid<string>>(result);
        Assert.Equal(4, invalid.Errors.Length);
    }

    [Fact]
    public void Apply_FiveFields_AccumulatesAllErrors()
    {
        Validated<Func<string, string, int, bool, double, string>> create =
            Valid<Func<string, string, int, bool, double, string>>((a, b, c, d, e) => "ok");

        var result = create
            .Apply(Invalid<string>("a", "e1"))
            .Apply(Invalid<string>("b", "e2"))
            .Apply(Invalid<int>("c", "e3"))
            .Apply(Invalid<bool>("d", "e4"))
            .Apply(Invalid<double>("e", "e5"));

        var invalid = Assert.IsType<Invalid<string>>(result);
        Assert.Equal(5, invalid.Errors.Length);
    }

    // -------------------------------------------------------------------------
    // LINQ query syntax
    // -------------------------------------------------------------------------

    [Fact]
    public void Select_Works_AsMap()
    {
        var result = from v in Valid(10)
                     select v * 2;

        Assert.Equal(20, Assert.IsType<Valid<int>>(result).Value);
    }

    [Fact]
    public void SelectMany_Works_AsBind()
    {
        var result = from x in Valid(10)
                     from y in Valid(20)
                     select x + y;

        Assert.Equal(30, Assert.IsType<Valid<int>>(result).Value);
    }

    [Fact]
    public void SelectMany_ShortCircuits_OnFirstError()
    {
        var result = from x in Invalid<int>("a", "err1")
                     from y in Invalid<int>("b", "err2")
                     select x + y;

        var invalid = Assert.IsType<Invalid<int>>(result);
        // Monadic: stops at first error
        Assert.Single(invalid.Errors);
        Assert.Equal("a", invalid.Errors[0].Field);
    }

    // -------------------------------------------------------------------------
    // Match
    // -------------------------------------------------------------------------

    [Fact]
    public void Match_Valid_InvokesValidBranch()
    {
        var result = Valid(42).Match(
            valid: v => $"value={v}",
            invalid: _ => "errors");

        Assert.Equal("value=42", result);
    }

    [Fact]
    public void Match_Invalid_InvokesInvalidBranch()
    {
        var result = Invalid<int>("f", "msg").Match(
            valid: _ => "ok",
            invalid: errors => errors[0].Message);

        Assert.Equal("msg", result);
    }

    // -------------------------------------------------------------------------
    // Traverse & Sequence
    // -------------------------------------------------------------------------

    [Fact]
    public void Traverse_AllValid_ReturnsValidCollection()
    {
        var inputs = new[] { "1", "2", "3" };
        var result = inputs.Traverse(s =>
            int.TryParse(s, out var n)
                ? Valid(n)
                : Invalid<int>("val", $"'{s}' is not a number"));

        var valid = Assert.IsType<Valid<IEnumerable<int>>>(result);
        Assert.Equal([1, 2, 3], valid.Value);
    }

    [Fact]
    public void Traverse_SomeInvalid_AccumulatesErrors()
    {
        var inputs = new[] { "1", "abc", "3", "xyz" };
        var result = inputs.Traverse(s =>
            int.TryParse(s, out var n)
                ? Valid(n)
                : Invalid<int>("val", $"'{s}' is not a number"));

        var invalid = Assert.IsType<Invalid<IEnumerable<int>>>(result);
        Assert.Equal(2, invalid.Errors.Length);
    }

    [Fact]
    public void Sequence_AllValid_ReturnsValidCollection()
    {
        var items = new Validated<int>[] { Valid(1), Valid(2), Valid(3) };
        var result = items.Sequence();

        var valid = Assert.IsType<Valid<IEnumerable<int>>>(result);
        Assert.Equal([1, 2, 3], valid.Value);
    }

    [Fact]
    public void Sequence_SomeInvalid_AccumulatesErrors()
    {
        var items = new Validated<int>[]
        {
            Valid(1),
            Invalid<int>("a", "err1"),
            Valid(3),
            Invalid<int>("b", "err2")
        };
        var result = items.Sequence();

        var invalid = Assert.IsType<Invalid<IEnumerable<int>>>(result);
        Assert.Equal(2, invalid.Errors.Length);
    }

    // -------------------------------------------------------------------------
    // Validate extension (compose validators on a single value)
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_AllPass_ReturnsValid()
    {
        var result = "hello".Validate(
            s => s.Length >= 1 ? Valid(s) : Invalid<string>("f", "too short"),
            s => s.Length <= 100 ? Valid(s) : Invalid<string>("f", "too long"));

        Assert.IsType<Valid<string>>(result);
    }

    [Fact]
    public void Validate_SomeFail_AccumulatesErrors()
    {
        var result = "".Validate(
            s => s.Length >= 1 ? Valid(s) : Invalid<string>("f", "too short"),
            s => s.Length <= 100 ? Valid(s) : Invalid<string>("f", "too long"),
            s => s.Contains('@') ? Valid(s) : Invalid<string>("f", "missing @"));

        var invalid = Assert.IsType<Invalid<string>>(result);
        // "too short" and "missing @" should fail; "too long" passes
        Assert.Equal(2, invalid.Errors.Length);
    }

    // -------------------------------------------------------------------------
    // Filtering
    // -------------------------------------------------------------------------

    [Fact]
    public void WhereValid_ExtractsValidValues()
    {
        var items = new Validated<int>[]
        {
            Valid(1), Invalid<int>("f", "e"), Valid(3)
        };

        var result = items.WhereValid().ToList();
        Assert.Equal([1, 3], result);
    }

    [Fact]
    public void WhereInvalid_ExtractsErrorArrays()
    {
        var items = new Validated<int>[]
        {
            Valid(1), Invalid<int>("f", "e"), Valid(3)
        };

        var result = items.WhereInvalid().ToList();
        Assert.Single(result);
        Assert.Equal("f", result[0][0].Field);
    }

    // -------------------------------------------------------------------------
    // Conversion (Validated ↔ Result)
    // -------------------------------------------------------------------------

    [Fact]
    public void ToResult_Valid_ReturnsOk()
    {
        var result = Valid(42).ToResult(errors => string.Join(", ", errors.Select(e => e.Message)));
        Assert.IsType<Ok<int, string>>(result);
    }

    [Fact]
    public void ToResult_Invalid_ReturnsError()
    {
        var result = Invalid<int>("f", "msg").ToResult(errors => string.Join(", ", errors.Select(e => e.Message)));
        var err = Assert.IsType<Error<int, string>>(result);
        Assert.Equal("msg", err.Value);
    }

    [Fact]
    public void ToValidated_Ok_ReturnsValid()
    {
        Result<int, string> ok = new Ok<int, string>(42);
        var result = ok.ToValidated(e => [new ValidationError("f", e)]);
        Assert.IsType<Valid<int>>(result);
    }

    [Fact]
    public void ToValidated_Error_ReturnsInvalid()
    {
        Result<int, string> err = new Error<int, string>("fail");
        var result = err.ToValidated(e => [new ValidationError("f", e)]);
        Assert.IsType<Invalid<int>>(result);
    }

    // -------------------------------------------------------------------------
    // Async Map / Bind
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AsyncMap_Valid_TransformsValue()
    {
        var result = await Task.FromResult(Valid(5)).Map(x => x * 2);
        Assert.Equal(10, Assert.IsType<Valid<int>>(result).Value);
    }

    [Fact]
    public async Task AsyncMap_Invalid_PreservesErrors()
    {
        var result = await Task.FromResult(Invalid<int>("f", "e")).Map(x => x * 2);
        Assert.IsType<Invalid<int>>(result);
    }

    [Fact]
    public async Task AsyncBind_Valid_ChainsValidation()
    {
        var result = await Task.FromResult(Valid(5))
            .Bind(x => Task.FromResult(x > 0 ? Valid(x) : Invalid<int>("f", "must be positive")));

        Assert.Equal(5, Assert.IsType<Valid<int>>(result).Value);
    }

    [Fact]
    public async Task AsyncBind_Invalid_ShortCircuits()
    {
        var result = await Task.FromResult(Invalid<int>("f", "e"))
            .Bind(x => Task.FromResult(Valid(x * 2)));

        Assert.IsType<Invalid<int>>(result);
    }

    // -------------------------------------------------------------------------
    // Async Traverse
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AsyncTraverse_Valid_TransformsAsynchronously()
    {
        var result = await Valid(5).Traverse(async x =>
        {
            await Task.Delay(1);
            return x * 2;
        });
        Assert.Equal(10, Assert.IsType<Valid<int>>(result).Value);
    }

    [Fact]
    public async Task AsyncTraverse_Invalid_PreservesErrors()
    {
        var result = await Invalid<int>("f", "e").Traverse(async x =>
        {
            await Task.Delay(1);
            return x * 2;
        });
        Assert.IsType<Invalid<int>>(result);
    }
}

// =============================================================================
// Curry Tests
// =============================================================================

public class CurryTests
{
    [Fact]
    public void First_TwoArg_CurriesCorrectly()
    {
        Func<int, int, int> add = (a, b) => a + b;
        var curried = Curry.First(add);
        Assert.Equal(3, curried(1)(2));
    }

    [Fact]
    public void First_ThreeArg_CurriesFirstArgument()
    {
        Func<int, int, int, int> add3 = (a, b, c) => a + b + c;
        var curried = Curry.First(add3);
        Assert.Equal(6, curried(1)(2, 3));
    }

    [Fact]
    public void First_FourArg_CurriesFirstArgument()
    {
        Func<int, int, int, int, int> add4 = (a, b, c, d) => a + b + c + d;
        var curried = Curry.First(add4);
        Assert.Equal(10, curried(1)(2, 3, 4));
    }

    [Fact]
    public void First_FiveArg_CurriesFirstArgument()
    {
        Func<int, int, int, int, int, int> add5 = (a, b, c, d, e) => a + b + c + d + e;
        var curried = Curry.First(add5);
        Assert.Equal(15, curried(1)(2, 3, 4, 5));
    }

    [Fact]
    public void Full_TwoArg_FullyCurries()
    {
        Func<int, int, int> add = (a, b) => a + b;
        var curried = Curry.Full(add);
        Assert.Equal(3, curried(1)(2));
    }

    [Fact]
    public void Full_ThreeArg_FullyCurries()
    {
        Func<int, int, int, int> add3 = (a, b, c) => a + b + c;
        var curried = Curry.Full(add3);
        Assert.Equal(6, curried(1)(2)(3));
    }
}

// =============================================================================
// Validators Tests
// =============================================================================

public class ValidatorsTests
{
    // -------------------------------------------------------------------------
    // Required / NonEmpty / NotNull
    // -------------------------------------------------------------------------

    [Fact]
    public void Required_NonNullString_IsValid()
    {
        var result = Validate.Required("hello", "name");
        Assert.IsType<Valid<string>>(result);
    }

    [Fact]
    public void Required_NullString_IsInvalid()
    {
        var result = Validate.Required(null, "name");
        Assert.IsType<Invalid<string>>(result);
    }

    [Fact]
    public void Required_EmptyString_IsInvalid()
    {
        var result = Validate.Required("", "name");
        Assert.IsType<Invalid<string>>(result);
    }

    [Fact]
    public void NonEmpty_WhitespaceOnly_IsInvalid()
    {
        var result = Validate.NonEmpty("   ", "name");
        Assert.IsType<Invalid<string>>(result);
    }

    [Fact]
    public void NonEmpty_ValidString_IsValid()
    {
        var result = Validate.NonEmpty("hello", "name");
        Assert.IsType<Valid<string>>(result);
    }

    [Fact]
    public void NotNull_Class_NonNull_IsValid()
    {
        var result = Validate.NotNull("hello", "field");
        Assert.IsType<Valid<string>>(result);
    }

    [Fact]
    public void NotNull_Class_Null_IsInvalid()
    {
        var result = Validate.NotNull((string?)null, "field");
        Assert.IsType<Invalid<string>>(result);
    }

    [Fact]
    public void NotNull_Struct_HasValue_IsValid()
    {
        var result = Validate.NotNull((int?)42, "field");
        Assert.Equal(42, Assert.IsType<Valid<int>>(result).Value);
    }

    [Fact]
    public void NotNull_Struct_Null_IsInvalid()
    {
        var result = Validate.NotNull((int?)null, "field");
        Assert.IsType<Invalid<int>>(result);
    }

    // -------------------------------------------------------------------------
    // MinLength / MaxLength
    // -------------------------------------------------------------------------

    [Fact]
    public void MinLength_LongEnough_IsValid()
    {
        var result = Validate.MinLength("hello", 3, "field");
        Assert.IsType<Valid<string>>(result);
    }

    [Fact]
    public void MinLength_TooShort_IsInvalid()
    {
        var result = Validate.MinLength("hi", 3, "field");
        Assert.IsType<Invalid<string>>(result);
    }

    [Fact]
    public void MinLength_ExactLength_IsValid()
    {
        var result = Validate.MinLength("hey", 3, "field");
        Assert.IsType<Valid<string>>(result);
    }

    [Fact]
    public void MaxLength_ShortEnough_IsValid()
    {
        var result = Validate.MaxLength("hi", 5, "field");
        Assert.IsType<Valid<string>>(result);
    }

    [Fact]
    public void MaxLength_TooLong_IsInvalid()
    {
        var result = Validate.MaxLength("hello world", 5, "field");
        Assert.IsType<Invalid<string>>(result);
    }

    [Fact]
    public void MaxLength_ExactLength_IsValid()
    {
        var result = Validate.MaxLength("hello", 5, "field");
        Assert.IsType<Valid<string>>(result);
    }

    // -------------------------------------------------------------------------
    // Email
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("user+tag@gmail.com")]
    public void Email_ValidAddresses_AreValid(string email)
    {
        var result = Validate.Email(email, "email");
        Assert.IsType<Valid<string>>(result);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@no-local.com")]
    [InlineData("")]
    public void Email_InvalidAddresses_AreInvalid(string email)
    {
        var result = Validate.Email(email, "email");
        Assert.IsType<Invalid<string>>(result);
    }

    // -------------------------------------------------------------------------
    // Pattern
    // -------------------------------------------------------------------------

    [Fact]
    public void Pattern_Matches_IsValid()
    {
        var regex = new System.Text.RegularExpressions.Regex(@"^\d{3}-\d{4}$");
        var result = Validate.Pattern("123-4567", regex, "phone");
        Assert.IsType<Valid<string>>(result);
    }

    [Fact]
    public void Pattern_DoesNotMatch_IsInvalid()
    {
        var regex = new System.Text.RegularExpressions.Regex(@"^\d{3}-\d{4}$");
        var result = Validate.Pattern("abc", regex, "phone", "Invalid phone number");
        var invalid = Assert.IsType<Invalid<string>>(result);
        Assert.Equal("Invalid phone number", invalid.Errors[0].Message);
    }

    // -------------------------------------------------------------------------
    // Range / Min / Max
    // -------------------------------------------------------------------------

    [Fact]
    public void Range_InRange_IsValid()
    {
        var result = Validate.Range(5, 1, 10, "age");
        Assert.IsType<Valid<int>>(result);
    }

    [Fact]
    public void Range_BelowMin_IsInvalid()
    {
        var result = Validate.Range(0, 1, 10, "age");
        Assert.IsType<Invalid<int>>(result);
    }

    [Fact]
    public void Range_AboveMax_IsInvalid()
    {
        var result = Validate.Range(11, 1, 10, "age");
        Assert.IsType<Invalid<int>>(result);
    }

    [Fact]
    public void Range_AtBoundaries_IsValid()
    {
        Assert.IsType<Valid<int>>(Validate.Range(1, 1, 10, "age"));
        Assert.IsType<Valid<int>>(Validate.Range(10, 1, 10, "age"));
    }

    [Fact]
    public void Min_AboveMin_IsValid()
    {
        var result = Validate.Min(5, 1, "val");
        Assert.IsType<Valid<int>>(result);
    }

    [Fact]
    public void Min_BelowMin_IsInvalid()
    {
        var result = Validate.Min(0, 1, "val");
        Assert.IsType<Invalid<int>>(result);
    }

    [Fact]
    public void Max_BelowMax_IsValid()
    {
        var result = Validate.Max(5, 10, "val");
        Assert.IsType<Valid<int>>(result);
    }

    [Fact]
    public void Max_AboveMax_IsInvalid()
    {
        var result = Validate.Max(11, 10, "val");
        Assert.IsType<Invalid<int>>(result);
    }

    // -------------------------------------------------------------------------
    // That (custom predicate)
    // -------------------------------------------------------------------------

    [Fact]
    public void That_PredicatePasses_IsValid()
    {
        var result = Validate.That(18, a => a >= 18, "age", "Must be 18 or older");
        Assert.IsType<Valid<int>>(result);
    }

    [Fact]
    public void That_PredicateFails_IsInvalid()
    {
        var result = Validate.That(16, a => a >= 18, "age", "Must be 18 or older");
        var invalid = Assert.IsType<Invalid<int>>(result);
        Assert.Equal("Must be 18 or older", invalid.Errors[0].Message);
    }

    // -------------------------------------------------------------------------
    // EqualTo
    // -------------------------------------------------------------------------

    [Fact]
    public void EqualTo_SameValues_IsValid()
    {
        var result = Validate.EqualTo("abc", "abc", "confirm");
        Assert.IsType<Valid<string>>(result);
    }

    [Fact]
    public void EqualTo_DifferentValues_IsInvalid()
    {
        var result = Validate.EqualTo("abc", "xyz", "confirm", "Values must match");
        var invalid = Assert.IsType<Invalid<string>>(result);
        Assert.Equal("Values must match", invalid.Errors[0].Message);
    }

    // -------------------------------------------------------------------------
    // Validate.All (combinator)
    // -------------------------------------------------------------------------

    [Fact]
    public void All_AllValid_ReturnsValid()
    {
        var result = Validate.All(
            Validate.NonEmpty("hello", "name"),
            Validate.MinLength("hello", 3, "name"),
            Validate.MaxLength("hello", 10, "name"));

        Assert.IsType<Valid<string>>(result);
    }

    [Fact]
    public void All_SomeInvalid_AccumulatesErrors()
    {
        var result = Validate.All(
            Validate.NonEmpty("", "name"),
            Validate.MinLength("", 3, "name"),
            Validate.MaxLength("", 10, "name"));

        var invalid = Assert.IsType<Invalid<string>>(result);
        // "NonEmpty" and "MinLength" fail; "MaxLength" passes
        Assert.Equal(2, invalid.Errors.Length);
    }

    // -------------------------------------------------------------------------
    // Integration: realistic form validation scenario
    // -------------------------------------------------------------------------

    private record User(string Name, string Email, int Age);

    [Fact]
    public void RealWorld_FormValidation_AllValid()
    {
        var name = "Alice";
        var email = "alice@example.com";
        var age = 25;

        var result = Valid<Func<string, string, int, User>>(
                (n, e, a) => new User(n, e, a))
            .Apply(Validate.NonEmpty(name, nameof(name)))
            .Apply(Validate.Email(email, nameof(email)))
            .Apply(Validate.Range(age, 18, 120, nameof(age)));

        var valid = Assert.IsType<Valid<User>>(result);
        Assert.Equal("Alice", valid.Value.Name);
        Assert.Equal("alice@example.com", valid.Value.Email);
        Assert.Equal(25, valid.Value.Age);
    }

    [Fact]
    public void RealWorld_FormValidation_MultipleErrors()
    {
        var name = "";
        var email = "not-an-email";
        var age = 10;

        var result = Valid<Func<string, string, int, User>>(
                (n, e, a) => new User(n, e, a))
            .Apply(Validate.NonEmpty(name, nameof(name)))
            .Apply(Validate.Email(email, nameof(email)))
            .Apply(Validate.Range(age, 18, 120, nameof(age)));

        var invalid = Assert.IsType<Invalid<User>>(result);
        Assert.Equal(3, invalid.Errors.Length);
        Assert.Contains(invalid.Errors, e => e.Field == nameof(name));
        Assert.Contains(invalid.Errors, e => e.Field == nameof(email));
        Assert.Contains(invalid.Errors, e => e.Field == nameof(age));
    }

    [Fact]
    public void RealWorld_ValidateExtension_ComposeValidators()
    {
        var email = "a@b.com";
        var result = email.Validate(
            e => Validate.NonEmpty(e, "email"),
            e => Validate.MaxLength(e, 255, "email"),
            e => Validate.Email(e, "email"));

        Assert.IsType<Valid<string>>(result);
    }

    [Fact]
    public void RealWorld_ValidateExtension_AllFail()
    {
        var email = "";
        var result = email.Validate(
            e => Validate.NonEmpty(e, "email"),
            e => Validate.Email(e, "email"));

        var invalid = Assert.IsType<Invalid<string>>(result);
        Assert.Equal(2, invalid.Errors.Length);
    }
}
