//
//  CLError.h
//  Cloud.com SDK
//
//  Created by David Bruck on 4/19/2013
//  Copyright © Cloud.com. All rights reserved.
//

/// <summary>
/// Class that represents an error, and supports logging of the error.  The error may contain one or more exceptions.
/// </summary>
class CLError

#begin region properties

/// <summary>
/// Returns the primary error code (includes the domain).
/// If the Code is CLExceptionCode.General_SeeFirstException, the user should inspect CLError.FirstException for the cause of the error.
/// This code is pulled from Exception.Code.
/// </summary>
 - CLExceptionCode Code ( getter only )

/// <summary>
/// Returns the primary error domain (calculated from the Code above).
/// This domain is pulled from Exception.Domain.
/// </summary>
 - CLExceptionDomain Domain ( getter only)

/// <summary>
/// This is the first exception that occurred in the standard platform exception class.
/// </summary>
 - Exception FirstException ( getter only)

/// <summary>
/// This is the complete exception information, perhaps including inner exceptions.
/// </summary>
 - CLException Exception ( getter only )

#end region properties

#begin region methods

/// <summary>
/// Logs error information from this CLError to the specified location if loggingEnabled parameter is passed as true
/// </summary>
/// <param name="logLocation">Base location for log files before date and extension are appended</param>
/// <param name="loggingEnabled">Determines whether logging will actually occur</param>
 - Log
	Parameters
	 - string logLocation
	 - bool loggingEnabled

	No Return

#end region methods