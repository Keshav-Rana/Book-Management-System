using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Utilities.Utility;

public class Utility
{
    //****************** Common Validations ****************************************
    // regex for DateTime validation
    private static readonly string patternForDateTimeValidation =
    @"^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01]) (\d{2}):([0-5][0-9]):([0-5][0-9])$";
    private static readonly Regex regexForDateTimeValidation = new Regex(patternForDateTimeValidation);

    // regex for Date Validation
    private static readonly string patternForDateValidation = @"^[0-9]{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$";
    private static readonly Regex regexForDateValidation = new Regex(patternForDateValidation);

    public static bool IsValidDateTime(DateTime datetime)
    {
        return regexForDateTimeValidation.IsMatch(datetime.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    public static bool IsValidDate(DateOnly date)
    {
        return regexForDateValidation.IsMatch(date.ToString("yyy-MM-dd"));
    }

    //****************** Book specific validations *********************************
    public static bool IsValidEdition(int? edition)
    {
        return edition > 0 && edition != null;
    }

    public static bool IsValidPrice(decimal? price)
    {
        return price >= 0 && price != null;
    }

    //****************** User specific validations *********************************
    public static bool IsValidEmail(string email)
    {
        // implement validation using MailAddress
        try
        {
            var mailAddress = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {

            return false;
        }
    }
    public static bool IsValidPassword(string password)
    {
        return password.Length > 8
        && password.Any(char.IsDigit)
        && password.Any(char.IsLower)
        && password.Any(char.IsUpper)
        && password.Distinct().Count() > 3
        // validate if the password contains a special character
        && password.Any(ch => !char.IsLetterOrDigit(ch));
    }
    public static bool IsValidRole(string role)
    {
        return role == "admin" || role == "customer";
    }
    public static bool IsValidStatus(string status)
    {
        return status == "active" || status == "inactive";
    }

    //******************** BorrowedBook specific Validations ************************
    public static bool IsValidBorrowStatus(string status)
    {
        return string.IsNullOrEmpty(status) || status == "returned";
    }

    public static bool IsValidReturnStatus(string status)
    {
        return status == "returned" || status == "overdue";
    }

    //******************** Review specific Validations ******************************
    public static bool IsValidRating(int? rating)
    {

        return rating >= 1 && rating <= 5;
    }
}