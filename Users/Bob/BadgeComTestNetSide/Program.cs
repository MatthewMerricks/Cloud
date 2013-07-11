using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.BadgeNET;
using Cloud.Static;
using Cloud;
using Cloud.Model;
using System.Threading.Tasks;
using System.Threading;
using Cloud.Support;

namespace BadgeComTestNetSide
{
    public class Program
    {
        // Constants
	    const int nMaxItemsAtLevel = 10;
	    const int nExplorersToSimulate = 1;
	    const int nMaxBadgeTypeToSimulate = (int)cloudAppIconBadgeType.cloudAppBadgeSynced;
        const int nMaxTestThreads = 4;

        static IconOverlay _iconOverlay = null;
        static CLSyncbox _syncbox = null;
        static string[,,] _paths = new string[nMaxItemsAtLevel, nMaxItemsAtLevel, nMaxItemsAtLevel];
        static Random _random = new Random();
        static CLTrace _trace = CLTrace.Instance;
        static Task[] _tasks = new Task[nMaxTestThreads];
        static CancellationTokenSource[] _cancelTokenSources = new CancellationTokenSource[nMaxTestThreads];
        static CancellationToken[] _cancelTokens = new CancellationToken[nMaxTestThreads];

        static void Main(string[] args)
        {
            // Initialize trace
            CLTrace.Initialize("C:\\Users\\Robertste\\AppData\\Local\\Cloud", "BadgeComTestNetSide", "log", 9, LogErrors: true);

            // Build the paths to test.
            FillPathArray("C:\\Users\\robertste\\Cloud", ref _paths);

            // Allocate the cancellation sources and tokens.
            for (int indexThread = 0; indexThread < nMaxTestThreads; indexThread++)
            {
                _cancelTokenSources[indexThread] = new CancellationTokenSource();
                _cancelTokens[indexThread] = _cancelTokenSources[indexThread].Token;
            }


            // Create credentials.
            CLCredentials credentials;
            CLError errorFromCredentialsAllocAndInit = CLCredentials.AllocAndInit("034e14679dfd54823e7d45b1e92dc7c95c57fa2b0430cc87462d82d678e7bec2", "7adb097a532be354c0aeccde9b9a3aaa5a0a8c2a6a9238d03b2dc9966e58086e", out credentials);
            if (errorFromCredentialsAllocAndInit != null)
            {
                _trace.writeToLog(9, "BadgeComTestNetSide: Main: ERROR: From CLCredentials.AllocAndInit: Msg: {0}.", errorFromCredentialsAllocAndInit.PrimaryException.Message);
                throw new AggregateException("Error creating credentials", errorFromCredentialsAllocAndInit.Exceptions);
            }

            // Create a syncbox.
            CLSyncbox syncbox;
            CLError errorFromSyncboxAllocAndInit = CLSyncbox.AllocAndInit(18, credentials, out syncbox, "C:\\Users\\Robertste\\Cloud");
            if (errorFromSyncboxAllocAndInit != null)
            {
                _trace.writeToLog(9, "BadgeComTestNetSide: Main: ERROR: From CLSyncbox.AllocAndInit: Msg: {0}.", errorFromSyncboxAllocAndInit.PrimaryException.Message);
                throw new AggregateException("Error creating syncbox", errorFromSyncboxAllocAndInit.Exceptions);
            }

            // Initialize to IconOverlay.
            _iconOverlay = new IconOverlay();
            CLError iconOverlayError = _iconOverlay.Initialize(syncbox.CopiedSettings, syncbox, null, false);
            if (iconOverlayError != null)
            {
                _trace.writeToLog(9, "BadgeComTestNetSide: Main: ERROR: From _iconOverlay.Initialize: Msg: {0}.", iconOverlayError.PrimaryException.Message);
                throw new AggregateException("Error initializing IconOverlay", iconOverlayError.Exceptions);
            }

            // From this point, we always need to shut down IconOverlay
            try
            {
                // Did IconOverlay initialize properly?
                bool fIsInitialized = false;
                CLError errorFromIsBadgingInitialized = _iconOverlay.IsBadgingInitialized(out fIsInitialized);
                if (errorFromIsBadgingInitialized != null || !fIsInitialized)
                {
                    _trace.writeToLog(9, "BadgeComTestNetSide: Main: ERROR: From _iconOverlay.IsBadgingInitialized: Msg: {0}.", errorFromIsBadgingInitialized.PrimaryException.Message);
                    throw new AggregateException("Error: IconOverlay did not initialize", errorFromIsBadgingInitialized.Exceptions);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(9, "BadgeComTestNetSide: Main: ERROR: Exception: Msg: {0}.", ex.Message);

                try
                {
                    // Shut down IconOverlay.
                    CLError errorFromShutdown = _iconOverlay.Shutdown();
                    if (errorFromShutdown != null)
                    {
                        _trace.writeToLog(9, "BadgeComTestNetSide: Main: ERROR: From _iconOverlay.Shutdown: Msg: {0}.", errorFromShutdown.PrimaryException.Message);
                        throw new AggregateException("Error shutting down IconOverlay", errorFromShutdown.Exceptions);
                    }
                }
                catch
                {
                }

                return;
            }

            // Start multiple threads.  Each will test using the same IconOverlay instance.
            for (int indexThread = 0; indexThread < nMaxTestThreads; indexThread++)
            {
                GenericHolder<int> indexHolder = new GenericHolder<int>(indexThread);

                _tasks[indexThread] = Task.Factory.StartNew((threadIndex) =>
                {
                    try
                    {
                        // Cast the input state.
                        GenericHolder<int> castState = threadIndex as GenericHolder<int>;
                        if (castState == null)
                        {
                            throw new NullReferenceException("castState must be a GenericHolder<int>");
                        }

                        // Run the test.
                        while (true)
                        {
                            _cancelTokens[castState.Value].ThrowIfCancellationRequested();

                            // Begin the actual test.  Choose the parameters randomly.
                            int index1 = _random.Next(nMaxItemsAtLevel);
                            int index2 = _random.Next(nMaxItemsAtLevel);
                            int index3 = _random.Next(nMaxItemsAtLevel);
                            bool willSetPath = _random.Next(100) > 3 ? true : false;
                            //TODO: The random high number is exclusive.  We should add one, but the Selective badge type has not been properly tested in IconOverlay.
                            int nBadgeType = _random.Next((int)cloudAppIconBadgeType.cloudAppBadgeSynced, (int)cloudAppIconBadgeType.cloudAppBadgeSyncSelective);
                            GenericHolder<cloudAppIconBadgeType> badgeType = new GenericHolder<cloudAppIconBadgeType>((cloudAppIconBadgeType)nBadgeType);
                            FilePath filePath = new FilePath(_paths[index1, index2, index3]);

                            // Make the call.
                            if (willSetPath)
                            {
                                CLError errorFromSetBadgeType = _iconOverlay.setBadgeType(badgeType, filePath, alreadyCheckedInitialized: true);
                                if (errorFromSetBadgeType != null)
                                {
                                    throw new AggregateException("Error from setBadgeType", errorFromSetBadgeType.Exceptions);
                                }
                            }
                            else
                            {
                                bool isDeleted;
                                CLError errorFromDeleteBadgePath =  _iconOverlay.DeleteBadgePath(filePath, out isDeleted, isPending: false, alreadyCheckedInitialized: true);
                                if (errorFromDeleteBadgePath != null)
                                {
                                    throw new AggregateException("Error from DeleteBadgePathe", errorFromDeleteBadgePath.Exceptions);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _trace.writeToLog(9, "BadgeComTestNetSide: Main: ERROR: Exception (2). Msg: {0}.", ex.Message);
                        return;
                    }
                    finally
                    {
                        try
                        {
                            // Shut down badging when the test exits.
                            CLError errorFromShutdown = _iconOverlay.Shutdown();
                            if (errorFromShutdown != null)
                            {
                                throw new AggregateException("Error shutting down IconOverlay", errorFromShutdown.Exceptions);
                            }
                        }
                        catch (Exception ex2)
                        {
                            _trace.writeToLog(9, "BadgeComTestNetSide: Main: ERROR: Exception (3). Msg: {0}.", ex2.Message);
                        }
                    }
                }, indexHolder, _cancelTokens[indexThread]);
            }

            // Wait for the user to tell us to stop.
            Console.WriteLine("Press any key to stop");
            Console.ReadLine();

            // Cancel the threads.
            for (int indexThread = 0; indexThread < nMaxTestThreads; indexThread++)
            {
                _cancelTokenSources[indexThread].Cancel();
            }

            // Wait for all of the threads to stop.
            try
            {
                Task.WaitAll(_tasks);
            }
            catch
            {
            }

            // Shut down IconOverlay
            try
            {
                CLError errorFromShutdown = _iconOverlay.Shutdown();
                if (errorFromShutdown != null)
                {
                    _trace.writeToLog(9, "BadgeComTestNetSide: Main: ERROR: From _iconOverlay.Shutdown (2): Msg: {0}.", errorFromShutdown.PrimaryException.Message);
                    throw new AggregateException("Error shutting down IconOverlay", errorFromShutdown.Exceptions);
                }
            }
            catch
            {
            }

            _trace.writeToLog(9, "BadgeComTestNetSide: Main: Exit.");
        }

        private static void FillPathArray(string leadIn, ref string[,,] paths)
        {
	        for (int index1 = 0; index1 < nMaxItemsAtLevel; index1++)
	        {
		        for (int index2 = 0; index2 < nMaxItemsAtLevel; index2++)
		        {
			        for (int index3 = 0; index3 < nMaxItemsAtLevel; index3++)
			        {

				        paths[index1, index2, index3] = leadIn + "\\Level1_LongLongName_" + index1.ToString() + "\\" + "Level2_LongLongName_" + index2.ToString() + "\\" + "Level3_LongLongName_" + index3.ToString() + ".txt";
			        }
		        }
	        }
        }
    }
}
