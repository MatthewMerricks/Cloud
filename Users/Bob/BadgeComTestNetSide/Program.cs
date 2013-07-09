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

namespace BadgeComTestNetSide
{
    public class Program
    {
	    const int nMaxItemsAtLevel = 10;
	    const int nExplorersToSimulate = 1;
	    const int nMaxBadgeTypeToSimulate = (int)cloudAppIconBadgeType.cloudAppBadgeSynced;

        static IconOverlay _iconOverlay = null;
        static CLSyncbox _syncbox = null;
        static string[,,] _paths = new string[nMaxItemsAtLevel, nMaxItemsAtLevel, nMaxItemsAtLevel];
        static Random _random = new Random();

        static void Main(string[] args)
        {
            // Start timeout thread
            var ts = new CancellationTokenSource();
            CancellationToken ct = ts.Token;
            var task = Task.Factory.StartNew(() =>
            {
                // Build the paths to test.
                FillPathArray("C:\\Users\\robertste\\Cloud", ref _paths);

                // Create credentials.
                CLCredentials credentials;
                CLError errorFromCredentialsAllocAndInit = CLCredentials.AllocAndInit("034e14679dfd54823e7d45b1e92dc7c95c57fa2b0430cc87462d82d678e7bec2", "7adb097a532be354c0aeccde9b9a3aaa5a0a8c2a6a9238d03b2dc9966e58086e", out credentials);
                if (errorFromCredentialsAllocAndInit != null)
                {
                    throw new AggregateException("Error creating credentials", errorFromCredentialsAllocAndInit.Exceptions);
                }

                // Create a syncbox.
                CLSyncbox syncbox;
                CLError errorFromSyncboxAllocAndInit = CLSyncbox.AllocAndInit(18, credentials, out syncbox, "C:\\Users\\Robertste\\Cloud");
                if (errorFromSyncboxAllocAndInit != null)
                {
                    throw new AggregateException("Error creating syncbox", errorFromSyncboxAllocAndInit.Exceptions);
                }

                // Initialize to IconOverlay.
                _iconOverlay = new IconOverlay();
                CLError iconOverlayError = _iconOverlay.Initialize(syncbox.CopiedSettings, syncbox, null, false);
                if (iconOverlayError != null)
                {
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
                        throw new AggregateException("Error: IconOverlay did not initialize", errorFromIsBadgingInitialized.Exceptions);
                    }

                    // Run the test.
                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();

                        // Begin the actual test.  Choose the parameters randomly.
                        int index1 = _random.Next(nMaxItemsAtLevel);
                        int index2 = _random.Next(nMaxItemsAtLevel);
                        int index3 = _random.Next(nMaxItemsAtLevel);
                        bool willSetPath = _random.Next(100) > 3 ? true : false;
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
                    catch
                    {
                    }
                }
            }, ct);

            // Wait for the user to tell us to stop.
            Console.WriteLine("Press any key to stop");
            Console.ReadLine();

            // Cancel the thread and wait for it to quit.
            ts.Cancel();
            try
            {
                task.Wait();
            }
            catch
            {
            }

        }

        private static void FillPathArray(string leadIn, ref string[,,] paths)
        {
	        for (int index1 = 0; index1 < nMaxItemsAtLevel; index1++)
	        {
		        for (int index2 = 0; index2 < nMaxItemsAtLevel; index2++)
		        {
			        for (int index3 = 0; index3 < nMaxItemsAtLevel; index3++)
			        {

				        paths[index1, index2, index3] = leadIn + "Level1_LongLongName_" + index1.ToString() + "\\" + "Level2_LongLongName_" + index2.ToString() + "\\" + "Level3_LongLongName_" + index3.ToString() + ".txt";
			        }
		        }
	        }
        }
    }
}
