'?' means nullable for non-reference types

//
//  CLHttpException.h
//  Cloud.com SDK
//
//  Created by David Bruck on 4/19/2013
//  Copyright © Cloud.com. All rights reserved.
//

/// <summary>
/// Derived CLException class to contain more specific information relating to Http errors, if provided
/// </summary>
class CLHttpException inherits CLException

#begin region properties

/// <summary>
/// Http status code returned from the server, if received
/// </summary>
 - HttpStatusCode? Status

/// <summary>
/// Response from the server in text format, if received
/// </summary>
 - string Response

#end region properties