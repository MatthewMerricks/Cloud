//
//  CLException.h
//  Cloud.com SDK
//
//  Created by David Bruck on 4/19/2013
//  Copyright © Cloud.com. All rights reserved.
//

/// <summary>
/// Derived AggregateException class to contain Cloud error domain and code
/// </summary>
class CLException inherits System.AggregateException

#begin region properties

/// <summary>
/// Domain of the error
/// </summary>
 - CLExceptionDomain Domain ( getter only )
 
/// <summary>
/// Specific code of the error, grouped by domain
/// </summary>
 - CLExceptionCode Code ( getter only )

#end region properties

#begin region System.AggregateException notable inherited properties

/// <summary>
/// Gets a message that describes the current exception.
/// </summary>
 - string Message ( getter only )
 
/// <summary>
/// Gets a read-only collection of the System.Exception instances that caused the current exception.
/// </summary>
 - ReadOnlyCollection<Exception> InnerExceptions ( getter only )

#end region System.AggregateException notable inherited properties