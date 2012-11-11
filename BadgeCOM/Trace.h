//
// Trace.h
// Cloud Windows COM
//
// Created By BobS
// Copyright (c) Cloud.com. All rights reserved.

#pragma once
#include <vector>
#include <iostream>
#include <fstream>

class Trace
{
private:
    static bool instanceFlag;
    static Trace *single;
	static CRITICAL_SECTION cs;
	std::string traceDirectory;
	FILE *traceStream;
	std::string traceFile;
	int maxPriorityToTrace;
	bool traceEnabled;

    Trace()
    {
        // Private constructor
		InitializeCriticalSection(&cs);

    }

public:
    static Trace* getInstance();
	void setDirectory(std::string traceDirectoryParm);
	void setMaxPriorityToTrace(int priority);
    void write(int priority, char *szFormat, ...);

    ~Trace()
    {
		if (instanceFlag)
		{
	        instanceFlag = false;
			DeleteCriticalSection(&cs);
		}
    }
};


