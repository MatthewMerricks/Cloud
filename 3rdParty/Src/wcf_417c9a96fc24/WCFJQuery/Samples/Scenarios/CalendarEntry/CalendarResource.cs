// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace CalendarEntry
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Json;
    using System.Net.Mail;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Web;

    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceContract]
    public class CalendarResource
    {
        private const int MaxFieldLength = 256;

        internal enum Units
        {
            Minutes,
            Hours,
            Days,
            Weeks
        }

        internal enum ShowMeAs
        {
            Busy,
            Available
        }

        [WebInvoke(UriTemplate = "", Method = "POST")]
        public JsonObject Post(JsonObject contact)
        {
            if (contact == null)
            {
                throw new ArgumentNullException("contact");
            }

            contact.ValidateStringLength("What", 1, MaxFieldLength)
                   .ValidateCustomValidator("StartDate", typeof(CustomValidator), "ValidateMeetingTime")
                   .ValidateStringLength("Where", 1, MaxFieldLength)
                   .ValidateStringLength("Description", MaxFieldLength)
                   .ValidateCustomValidator("ReminderValue", typeof(CustomValidator), "ValidateReminder")
                   .ValidateEnum("ShowMeAs", typeof(ShowMeAs))
                   .ValidateCustomValidator("Guests", typeof(CustomValidator), "ValidateGuestEmails");

            string modifyEventName = "ModifyEvent";
            if (contact.ContainsKey(modifyEventName))
            {
                contact.ValidateTypeOf<bool>(modifyEventName);
            }

            string inviteOthersName = "InviteOthers";
            if (contact.ContainsKey(inviteOthersName))
            {
                contact.ValidateTypeOf<bool>(inviteOthersName);
            }

            string seeGuestListName = "SeeGuestList";
            if (contact.ContainsKey(seeGuestListName))
            {
                contact.ValidateTypeOf<bool>(seeGuestListName);
            }

            return new JsonObject();
        }

        private static class CustomValidator
        {
            public static ValidationResult ValidateMeetingTime(JsonValue jv, ValidationContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }
                
                JsonObject contact = (JsonObject)context.ObjectInstance;
                DateTime start, end, startTime, endTime;

                string startDateName = "StartDate";
                string endDateName = "EndDate";
                string startTimeName = "StartTime";
                string endTimeName = "EndTime";

                try
                {
                    contact.ValidatePresence(startDateName).ValidateTypeOf<DateTime>(startDateName);
                    start = contact[startDateName].ReadAs<DateTime>();

                    contact.ValidatePresence(endDateName).ValidateTypeOf<DateTime>(endDateName);
                    end = contact[endDateName].ReadAs<DateTime>();

                    contact.ValidateTypeOf<DateTime>(startTimeName);
                    startTime = contact[startTimeName].ReadAs<DateTime>();

                    contact.ValidateTypeOf<DateTime>(endTimeName);
                    endTime = contact[endTimeName].ReadAs<DateTime>();
                }
                catch (ValidationException ex)
                {
                    return ex.ValidationResult;
                }

                start = new DateTime(start.Year, start.Month, start.Day, startTime.Hour, startTime.Minute, startTime.Second, DateTimeKind.Local);
                end = new DateTime(end.Year, end.Month, end.Day, endTime.Hour, endTime.Minute, endTime.Second, DateTimeKind.Local);
                end.AddHours(endTime.Hour);
                end.AddMinutes(endTime.Minute);
                end.AddSeconds(endTime.Second);

                if (end <= start)
                {
                    return new ValidationResult("The start date needs to be before the end date.", new List<string> { startDateName });
                }

                return ValidationResult.Success;
            }

            public static ValidationResult ValidateReminder(JsonValue jv, ValidationContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }

                JsonObject contact = (JsonObject)context.ObjectInstance;

                string reminderValueName = "ReminderValue";
                contact.ValidateTypeOf<int>(reminderValueName);
                int reminder = contact[reminderValueName].ReadAs<int>();

                string reminderUnitsName = "ReminderUnits";
                contact.ValidateEnum(reminderUnitsName, typeof(Units));
                Units units = (Units)Enum.Parse(typeof(Units), contact[reminderUnitsName].ReadAs<string>());

                int maxReminder;
                switch (units)
                {
                    case Units.Hours:
                        maxReminder = 48;
                        break;
                    case Units.Days:
                        maxReminder = 2;
                        break;
                    case Units.Weeks:
                        maxReminder = 2;
                        break;
                    default:
                        maxReminder = 90;
                        break;
                }

                if (reminder > maxReminder)
                {
                    return new ValidationResult("This value is longer than the maximum allowed.", new List<string> { reminderValueName });
                }

                return ValidationResult.Success;
            }

            public static ValidationResult ValidateGuestEmails(JsonValue jv, ValidationContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }

                JsonObject contact = (JsonObject)context.ObjectInstance;
                string guestsName = "Guests";

                List<MailAddress> result = new List<MailAddress>();
                string[] parts = contact[guestsName].ReadAs<string>().Trim().Split(',');

                foreach (string address in parts)
                {
                    if (String.IsNullOrEmpty(address))
                    {
                        continue;
                    }

                    try
                    {
                        MailAddress parsed = new MailAddress(address);
                        result.Add(parsed);
                    }
                    catch (FormatException)
                    {
                        return new ValidationResult("This field contains an improperly formatted email address.", new List<string> { context.MemberName });
                    }
                }

                return ValidationResult.Success;
            }
        }
    }
}
