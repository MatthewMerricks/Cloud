o For using Windows shared memory, comment out line 18 of 3rdParty\src\boost_1_52\boost\interprocess\detail\workaround.hpp.
                   //RKS#define BOOST_INTERPROCESS_FORCE_GENERIC_EMULATION
o Bug reported here: http://web.archiveorange.com/archive/v/NDiIbZxvzjzYKvCoLZZB
  I applied this:
  - In boost/interprocess/sync/windows/semaphore.hpp, line 55.  Initialized bool open_or_created to false.
  - In boost/interprocess/sync/windows/mutex.hpp, line 57.  Initialized bool open_or_created to false.
 
