//  Migration2.cs
//  Cloud Windows
//
//  Created by David Bruck.
//  Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;

namespace Cloud.SQLIndexer.Migrations
{
    internal class Migration2 : IMigration
    {
        public static Migration2 Instance = new Migration2();

        private Migration2() { }

        #region IMigration member
        public void Apply(SqlCeConnection connection, string indexDBPassword)
        {
            Array.ForEach(new string[]
            {
                "JOyJDprfb6pCaJjyoaPAAsRFi7UmB5Kilv2A+csSLDPkk0I0Ms7MZiQcEJjMdqVGGaUcn7OFAvyIgU1Y4SIaTMxWWDLC5F+/HrYXssvgJvXqSutRGBIvxk52aenKjOZauxeDYdrWK//M4Fal7lhTNb0lyxfnwa2tea8pt2sNIJqFy52FWl30MhzbDgWtRGEm",
                "JOyJDprfb6pCaJjyoaPAAsRFi7UmB5Kilv2A+csSLDPkk0I0Ms7MZiQcEJjMdqVGGaUcn7OFAvyIgU1Y4SIaTMxWWDLC5F+/HrYXssvgJvWZfcUn1Hh7Rfi1aVXUUlg5D1tx8EcAuZia8DG1vfIL04EKvBojtUWdnSZckf2liHnEEP01oCqEcA6ifxitRGTony73U+ABaAGFpKRFcyAGLO9soIO1ND55JtRJnA7fipY=",
                "i6sBEUwt5AiF30uujbzeXrfqf5sBAhagjWraXh71OfyEqhPXSwpDBuqkrKNMq5qECDOOEIGfMZxFX/S0afBnCjIgPR1ivwwtMvyCIdbENQov49oBqFkEVeRzQc+mBNIjm/yLyD3rCBrsocIyfK9oIw==",
                "JOyJDprfb6pCaJjyoaPAAtNHg+d0nEEOXxycEx8n0dC0vD8kp2T/QejI2xPtuCXDVk4xlIec4kx67nNsi0C6bKPA3FiKVYVKddPT0yk7/oGcmUAJPamnFkSMrwIJdYnKmxTo7Q2nJPs7U/lG84kQZDKlMu19d9ltctog1MXFSL0=",
                "JOyJDprfb6pCaJjyoaPAAtNHg+d0nEEOXxycEx8n0dC0vD8kp2T/QejI2xPtuCXDVk4xlIec4kx67nNsi0C6bMYgvkJJbIbEjGVZVGOr0pq1YiwrsT7GUMDPzci8Ewax66oxPlLhrqjq8HF4Mv3qOL1nTy1f/bOixMbYtOWjD1Q=",
                "JOyJDprfb6pCaJjyoaPAAtNHg+d0nEEOXxycEx8n0dC0vD8kp2T/QejI2xPtuCXDVk4xlIec4kx67nNsi0C6bCYwMQPYDpsKNmVyqlalrt3ALUAAE/M665MhABqsGoMJhIrbp+TRwHbRnwGr/x74bOnmg3e7OwBFpmXTylAqTmM=",
                "LsdPv0vBjwksqSjBCO0Y2lMx8+mQa/fZbNF8rdTBpStRyfF76nUtCHgjJe+z7thQ7cJ2PJnq9v0tameffBNoz3CfaGhcUss2X4vBeCaljjJ5SUJ+QKVo+mETdtkGmP5XfHrezDJ1DTlxbRpmJXIH9g==",
                "JOyJDprfb6pCaJjyoaPAAtNHg+d0nEEOXxycEx8n0dC0vD8kp2T/QejI2xPtuCXDVk4xlIec4kx67nNsi0C6bFiW6v4XXKgY3aWRr/2y1Mnl4BBPU8dBiZtu0I8T/DfDdPTokoXN5nfWRUqqMCvDQdCP8aIFIixJlVsa4rp9r8C/CLagiv+oUotz+2PK7+mLpwpQ3n5j5Ug9OoZ5OlFS3Q=="
            }, currentScript =>
                {
                    using (SqlCeCommand runScriptCommand = connection.CreateCommand())
                    {
                        runScriptCommand.CommandText = Helpers.DecryptString(currentScript, indexDBPassword);
                        runScriptCommand.ExecuteNonQuery();
                    }
                });

            string fileSystemObjectsPrimaryKeyName = null;
            string fileSystemObjectsPrimaryKeyColumn = null;
            using (SqlCeCommand fileSystemObjectsKeyCommand = connection.CreateCommand())
            {
                fileSystemObjectsKeyCommand.CommandText = Helpers.DecryptString("MBkucheq1xqUJX6uYUrYUMGU4ftdKMhe2rEPLvDWiQ/gfZAxHTxxSFmTaHasvRIgZCzrqJoyQeMWUj0Z2D0AUlS3jaG+QoyiVM3oAP0CvDdGyA0TvcSuCcO9Am3g5QbC8xFAOTkzz5sACG2+1dOxvwGv4b9oXGqZMis5/Qhjnw65DHYTWO8QqRkJQnJP536BAneOvHgut7J7AUac1d0et2ZPDntp+0g5rhA38B7MLLc9EgRWCPCZv7tepjJ3yfbi2UDtHhqjiiBzBzwD3uh7cnx3xUJwVoDVuOhSMwzlOgU+iXhW/QDglbpN/633Gh5gTDFQpXUBVNAUtoBhSiz51R4pja2UPTNI7uKs1pI7P7RtyuKq003wXFdhcOcM7iKJeyx4AMzcQAGk92qJHc2G/5AoHKEgvIjZsiwTBEgZQWyO8I50fN+wqSm46b2schmQBmZ+KLO9+1pvcm8RN158i6WFh+yMWt93WJqqgwOKZv1aIdHi2LSJ3UP3McItZtp8Pvy5atxbUDXcSkkH9rxb3yF1sa52ibocO6Yd9z6/3c24KP/tqlLZ1O/uG8gePNp0Mqz5vwe5PIp7aTEtGSuH+fuLj4KKcr/t3SI+3obyUFA7fExdw4wy7oAXRfgmmKj6XCVJgybKSWSwhuhU0TEt11B2MXsh4VWUfreUIdR0+hj9FmG18uwLDUNqJ+dvv7/wJR/sSSOwysQy+Z/Lf+7B9A==", indexDBPassword);

                using (SqlCeResultSet fileSystemObjectsKeyResults = fileSystemObjectsKeyCommand.ExecuteResultSet(ResultSetOptions.Scrollable))
                {
                    foreach (SqlCeUpdatableRecord currentFileSystemObjectsKey in fileSystemObjectsKeyResults.OfType<SqlCeUpdatableRecord>())
                    {
                        fileSystemObjectsPrimaryKeyName = Convert.ToString(currentFileSystemObjectsKey["INDEX_NAME"]);
                        fileSystemObjectsPrimaryKeyColumn = Convert.ToString(currentFileSystemObjectsKey["COLUMN_NAME"]);
                    }
                }
            }

            string fileSystemObjectsName = Helpers.DecryptString("X3gTX9ZUTwdMPR5XpNgfMK4lan7eGpNMYCHiBLCqFtUiMAUKMYsGBhtsAJ6Hn9B/", indexDBPassword);

            using (SqlCeCommand syncStateReaderCommand = connection.CreateCommand())
            using (SqlCeCommand syncCounterWriterCommand = connection.CreateCommand())
            {
                syncStateReaderCommand.CommandType = CommandType.TableDirect;
                syncStateReaderCommand.CommandText = Helpers.DecryptString("KPM6wcQ9c0PVksLZt+02QVSc9cCP6Fz6WajK+fnW5Po=", indexDBPassword);

                syncCounterWriterCommand.CommandType = CommandType.TableDirect;
                syncCounterWriterCommand.CommandText = fileSystemObjectsName;
                syncCounterWriterCommand.IndexName = fileSystemObjectsPrimaryKeyName;

                using (SqlCeDataReader syncStatesReader = syncStateReaderCommand.ExecuteReader(CommandBehavior.SingleResult))
                using (SqlCeResultSet syncCounterWriter = syncCounterWriterCommand.ExecuteResultSet(ResultSetOptions.Sensitive | ResultSetOptions.Scrollable | ResultSetOptions.Updatable))
                {
                    string counterName = Helpers.DecryptString("rBo9WSY76jK45ckcBwEI1TUjYyiC0cZkv3pyp1TdVBc=", indexDBPassword);
                    int fromCounterOrdinal = syncStatesReader.GetOrdinal(counterName);
                    int toCounterOrdinal = syncCounterWriter.GetOrdinal(counterName);

                    while (syncStatesReader.Read())
                    {
                        if (syncCounterWriter.Seek(DbSeekOptions.FirstEqual, syncStatesReader[fileSystemObjectsPrimaryKeyColumn])
                            && syncCounterWriter.Read())
                        {
                            syncCounterWriter.SetValue(toCounterOrdinal, syncStatesReader.GetValue(fromCounterOrdinal));
                        }

                        syncCounterWriter.Update();
                    }
                }
            }

            Nullable<long> latestSyncCounter = null;
            using (SqlCeCommand highestSyncCommand = connection.CreateCommand())
            {
                highestSyncCommand.CommandText = Helpers.DecryptString("XvBsus46xjQXebDetQqrOuMaajXNk33DSV0h7vFhNDG2+2Cgt+n6Fc4b6aASgxRefvpANfsvx0EN5IrZrcjMDyJLWpRCqme0z94p8Z2UzBp3lJajN9MihIkBpiXrBIrA7+ezQw+XxFogfv592G7dTglAAYtl4clQs9XF4tNKdADDReYz86cOVHvfrrReOWXJS8iN9nlHhttF1TZBmxeAIn+JtZO/SrahRDI5Y9NnaGQ=", indexDBPassword);
                using (SqlCeResultSet highestSyncResults = highestSyncCommand.ExecuteResultSet(ResultSetOptions.Scrollable))
                {
                    foreach (SqlCeUpdatableRecord currentHighestSync in highestSyncResults.OfType<SqlCeUpdatableRecord>())
                    {
                        latestSyncCounter = Convert.ToInt64(currentHighestSync[0]);
                    }
                }
            }

            using (SqlCeCommand eventReaderCommand = connection.CreateCommand())
            using (SqlCeCommand eventIdWriterCommand = connection.CreateCommand())
            {
                eventReaderCommand.CommandType = CommandType.TableDirect;
                eventReaderCommand.CommandText = Helpers.DecryptString("hDjtL70SBHCq2QBHfWg3FA==", indexDBPassword);
                eventReaderCommand.IndexName = Helpers.DecryptString("od1dG1Nruc99KBgGIkrODqa2xtkGIXLTf1uH2R2fsnGNrgD+TQtSPjeCJnnE65+w", indexDBPassword);

                eventIdWriterCommand.CommandType = CommandType.TableDirect;
                eventIdWriterCommand.CommandText = fileSystemObjectsName;
                eventIdWriterCommand.IndexName = fileSystemObjectsPrimaryKeyName;

                using (SqlCeDataReader eventReader = eventReaderCommand.ExecuteReader(CommandBehavior.SingleResult))
                using (SqlCeResultSet eventIdWriter = eventIdWriterCommand.ExecuteResultSet(ResultSetOptions.Sensitive | ResultSetOptions.Scrollable | ResultSetOptions.Updatable))
                {
                    string eventIdName = Helpers.DecryptString("jtGGrbEvfImk217p+JAYXA==", indexDBPassword);
                    int fromIdOrdinal = eventReader.GetOrdinal(eventIdName);
                    int toIdOrdinal = eventIdWriter.GetOrdinal(eventIdName);

                    if (eventReader.Seek(DbSeekOptions.FirstEqual, (latestSyncCounter == null ? (object)DBNull.Value : (long)latestSyncCounter)))
                    {
                        while (eventReader.Read())
                        {
                            if (eventIdWriter.Seek(DbSeekOptions.FirstEqual, eventReader[fileSystemObjectsPrimaryKeyColumn])
                                && eventIdWriter.Read())
                            {
                                eventIdWriter.SetValue(toIdOrdinal, eventReader.GetValue(fromIdOrdinal));
                            }
                        }

                        eventIdWriter.Update();
                    }
                }
            }

            Array.ForEach(new string[]
            {
                "i6sBEUwt5AiF30uujbzeXrfqf5sBAhagjWraXh71OfyEqhPXSwpDBuqkrKNMq5qECDOOEIGfMZxFX/S0afBnCu6SZXUCpVNmUoamgqyNdvSTYWuQDmgAkOJnHiOABPYa",
                "LPe8ox3pydzzT+dh3OcJx0oinbrHlC4Xm30xhxneQh8UQqNs9f7cv3mUrRYa9DmE",
                "DayXrVp82vo5uJiMl2RKHj3nheFmTUIiNT9zY6CfwPl4LMJqPuU1FaEzhJXdTJVsLBCyywTH3gm0FTESyKmqnF16g4gH4qdUmR/SxOJZ+VEXtDJWA9K5Sq37LESOURnNb/gF4CQ40XvfLNaLBfB8rPSv1P6ccx+RZTALQCupegHAJsaxb1vlaVFftnM6CE3+DytLi2dARs/ywjC7KEHxOyEMoDlkb/lckX5PukvmDlY+eReM/m68Fvwxm3troqmyJMQnimU3zqfCuKuYC7rMjMbVPa7T1entfbCxUhDRS1ZCGjxyUfn+l3IbPteVpIHDGE4qnRtB+e1kituEqG74J4RJWKTyidozY6AUGuXhKTZwWWY7Dhp2ccgby1CWzp48KXS4itStQmW7gVW1K9u6nA==",
                "DayXrVp82vo5uJiMl2RKHj3nheFmTUIiNT9zY6CfwPl4LMJqPuU1FaEzhJXdTJVsLBCyywTH3gm0FTESyKmqnF16g4gH4qdUmR/SxOJZ+VEXtDJWA9K5Sq37LESOURnN9VBbs3Ja6AiOH9Jq+oVkmxIC1LOMMTV0UIYluRJe6iWWVZHKOjvrfs1docrMgIUtHcjIYsGkVwFdQVVExwtR6pURzP6U0tDWhI2HI+j7lFDQOe9K/14e3TrURH+5/s3er2DjASqsqFTRUVo8skM5nw==",
                "JOyJDprfb6pCaJjyoaPAAtNHg+d0nEEOXxycEx8n0dC0vD8kp2T/QejI2xPtuCXDVk4xlIec4kx67nNsi0C6bCaMxz5KjswZ/Rfxi/f/fagBknHYv1+c7CtHwFXJVDagnatWHNlBnaIil8N1iyDSUCSCKTyMogBbmSoW6FbSkAIMSbw+kROyGydkDWlEnU1nK73th1gByqX6hqKxSvzz+hZ53qku7aaqnirSbGvHnwgQ9/iNCNreE61tXbsuMHikgz7Uhl5kau8w9IunYN9aKtLZGee9sBeTOMQLUUkSiRKOZHPOgYJMx5h0DmFhEJ4iwxlRBL7+LjaA0ZhdsST4o1N3RWEA6FV6n11y9dsmMVStVNWlOBG0pgswf/kJ8Lsf",
                "JOyJDprfb6pCaJjyoaPAAtNHg+d0nEEOXxycEx8n0dC0vD8kp2T/QejI2xPtuCXDVk4xlIec4kx67nNsi0C6bCaMxz5KjswZ/Rfxi/f/fagBknHYv1+c7CtHwFXJVDaggi20cIykhAD/CYQqM0VCk1fRc72OxBGCcW9DgIoN4xWyp68eWkaW58W8HbNLkjqv/Z3vtENB21DHIYqIhtP7PhGXeffccU2Vk9hXocgxt5wSX/RnEKEnmxLTAuM2qCbDnKvhLs78u+YKjN1XkDjAnnbJYhCl5lFLBh4gYRlbOKzuAy5eu+4HHVT1ifw7teSWGC7K/E9l5U1rT/Xr8BT3sg==",
                "cOC18S2MW0uvRWGOZzLsuVmgdfNqXxmtUDt4JoW4tQYXA+wZ31r6hqPMKYf2dGhwjuW3DTEk2pNkznz0ZLSsCn3fNMAFSdgHCIqqgsUfS8Y2H3l7kc6dEuFmEOJ+SOn6ly6xYaIYwwSFIrVdvxQhs742TEPIkuMiQtkMdpoDJpc0x9hAMKbJVM/Qwpu/bWl8VMpUqSqqBNqBKdY1eLDwe3nD3QxvhXxCZy51rTczcclBmidVxi7aPuGFOe1OGjtX7Zuj9TUmrW3pUz8JeC0doWOD/r5XyMCeKRZvh1aHNxIDqQb+3Mjn0oquSG5s3Dft/occOZE0wYkSeOxUsgJvvw==",
                "JOyJDprfb6pCaJjyoaPAAsRFi7UmB5Kilv2A+csSLDPkk0I0Ms7MZiQcEJjMdqVGDTSXRZpD4yKPD74qRBlj4sfJQSRZ5j7FFUkZEBUIVZHrwne9Ss6C8eKDbD9g9E8sUigdGHV/VAk9YQA2KVEAOg==",
                "JOyJDprfb6pCaJjyoaPAAsRFi7UmB5Kilv2A+csSLDPkk0I0Ms7MZiQcEJjMdqVGDTSXRZpD4yKPD74qRBlj4knbVcvcJ1acTz6cGmBaIzjBZwRpP2iqUqM9/vgxO3ja",
                "i6sBEUwt5AiF30uujbzeXtKKGfzM5RhRoJ4RJeb5Dl8UiT5/f93RPp5fbHglYEjGVMehORADNpzCppeHH+XE1CNnmfJxEVHio3v4iTWsgCB2cRMVYJZzgl5FAyQr0VDkdukQ/CMHAKmHXlD3uDJA3uCe1WNcs5Z+pn8jT+1a2tM=",
                "JOyJDprfb6pCaJjyoaPAAtNHg+d0nEEOXxycEx8n0dC0vD8kp2T/QejI2xPtuCXDVk4xlIec4kx67nNsi0C6bFiW6v4XXKgY3aWRr/2y1MmjtBogg+moG0Zl5lPs5SyDqCTDE/nC4BP08IphIeXGsF04KOfWtUCvLEijwJaAtYYwCjmKvC/WwH97vQGYcan9ekarOzGd+j6wSG2O0cTQdQ==",
                "LsdPv0vBjwksqSjBCO0Y2lMx8+mQa/fZbNF8rdTBpStRyfF76nUtCHgjJe+z7thQ7cJ2PJnq9v0tameffBNoz01YcLvcHdsmTQvgXO9NPDuu5LLCucxrMsTkZN8d8BOIvJmon/eglIS5ngqNJKRutOWcIPZfjd8uBbyYTg5NsW1sXgi9sSCxJh4FBSlEx7Zba29ZJjLBVdz5bRAktQJlwJJYRn4ivPhF4mzJ5ELOmoul2flecYrevPRzG8c80TW95K/PahA1+eF+bhV0vjdhwjAnjdQARvAwJeE/0lv0Iwex6H8UjiqxQKZDpocP58pa",
                "DayXrVp82vo5uJiMl2RKHj3nheFmTUIiNT9zY6CfwPl4LMJqPuU1FaEzhJXdTJVsLBCyywTH3gm0FTESyKmqnF16g4gH4qdUmR/SxOJZ+VEXtDJWA9K5Sq37LESOURnNVq18fhvtKFTvMlXjnTrLo68NzdLLk2nWja0U6aFxCeCDiLfHN83w+fzpKN6kMyCC64LkrxG+cZmaaGfC8A17J7Ifptn75eGwT5nH6JUxtqONXOWeXbwolzG2fOP1NhrH6qX68x8pppgi8PZmAFrhyBbiCjqE2zT0i3zuT/+Rgjw=",
                "JOyJDprfb6pCaJjyoaPAAtNHg+d0nEEOXxycEx8n0dC0vD8kp2T/QejI2xPtuCXDVk4xlIec4kx67nNsi0C6bPsU7L6rAahAi1s59qb4RoUFx9gRxdKwWgQbr1P0xMRsX+ByUEG5Psp6eWcpsz74v1oFnAigjnfOypwBKN901GY=",
                "JOyJDprfb6pCaJjyoaPAAsRFi7UmB5Kilv2A+csSLDMM5janF9SwDl0b++EQjOBa+wnnwDiKM7N4CbrRYKBQFmZAALXtnPO0DXpNqsiOHdvSdNzHQEHnXLCzL8yQ9G4dt0XwBT3Sm53vrbBRjplstKdh1+x0ybpYoztBSzNfGqg=",
                "JOyJDprfb6pCaJjyoaPAAsRFi7UmB5Kilv2A+csSLDMM5janF9SwDl0b++EQjOBa+wnnwDiKM7N4CbrRYKBQFqPt/f5Hdu4yHttEI16/TQarbd2szz26bdXzXOzfWoAe",
                "DayXrVp82vo5uJiMl2RKHj3nheFmTUIiNT9zY6CfwPl4LMJqPuU1FaEzhJXdTJVsCCOfAc9IC1tI+ei3N6EWXgnCsTADQfx31zW0aj7WykZvvFjD5wo236oIOCjvdm78fNZ1d413qzM0eI2qmsK/4wYgyxWJxJNRZBXDQpuo3haqg+mRMoufJNYcfBo7MuTKfjFuFTNYN6LSrZKhT+GhP/K+ZX99KqH2vyfF83BWQw0="
            }, currentScript =>
            {
                using (SqlCeCommand runScriptCommand = connection.CreateCommand())
                {
                    runScriptCommand.CommandText = Helpers.DecryptString(currentScript, indexDBPassword);
                    runScriptCommand.ExecuteNonQuery();
                }
            });

            string eventsPrimaryKeyName = null;
            string eventsPrimaryKeyColumn = null;
            using (SqlCeCommand eventsKeyCommand = connection.CreateCommand())
            {
                eventsKeyCommand.CommandText = Helpers.DecryptString("MBkucheq1xqUJX6uYUrYUMGU4ftdKMhe2rEPLvDWiQ/gfZAxHTxxSFmTaHasvRIgZCzrqJoyQeMWUj0Z2D0AUlS3jaG+QoyiVM3oAP0CvDdGyA0TvcSuCcO9Am3g5QbC8xFAOTkzz5sACG2+1dOxvwGv4b9oXGqZMis5/Qhjnw65DHYTWO8QqRkJQnJP536BAneOvHgut7J7AUac1d0et2ZPDntp+0g5rhA38B7MLLc9EgRWCPCZv7tepjJ3yfbi2UDtHhqjiiBzBzwD3uh7cnx3xUJwVoDVuOhSMwzlOgU+iXhW/QDglbpN/633Gh5gTDFQpXUBVNAUtoBhSiz51R4pja2UPTNI7uKs1pI7P7RtyuKq003wXFdhcOcM7iKJeyx4AMzcQAGk92qJHc2G/5AoHKEgvIjZsiwTBEgZQWyO8I50fN+wqSm46b2schmQBmZ+KLO9+1pvcm8RN158i6WFh+yMWt93WJqqgwOKZv1aIdHi2LSJ3UP3McItZtp8k55gPF1U3BEwooDWbKSxMDxi1SQEewoQLaHWno18Hf6Lpbhj68kMjWEAVho0Kt3HL6THylRwgyumV/jEljHpHM18LTnPcaLFgbF9tgtVaMJU8uIw1c0dhXu5Gk6JOJq/r0JWIuN6K6RQTIZAw29gFRQjLqCLA2VeRg8Ex5buVXZw5ZqvBkMgpYe+aGq6oFAU", indexDBPassword);

                using (SqlCeResultSet eventsKeyResults = eventsKeyCommand.ExecuteResultSet(ResultSetOptions.Scrollable))
                {
                    foreach (SqlCeUpdatableRecord currentEventKey in eventsKeyResults.OfType<SqlCeUpdatableRecord>())
                    {
                        eventsPrimaryKeyName = Convert.ToString(currentEventKey["INDEX_NAME"]);
                        eventsPrimaryKeyColumn = Convert.ToString(currentEventKey["COLUMN_NAME"]);
                    }
                }
            }

            using (SqlCeCommand orphanedEventsCommand = connection.CreateCommand())
            using (SqlCeCommand deleteOrphansCommand = connection.CreateCommand())
            {
                orphanedEventsCommand.CommandText = Helpers.DecryptString("MBkucheq1xqUJX6uYUrYUKkRwl8QC5HDFqIUqAg727vMRb7Odlwr6hEeM8oylY8KyzG/I1W9WDZWh1JOSEj5828C2AKicEPA4foMPFsGHpqbRgZFecmpAvTFqeWUP4svP1k5Gzj5/P0VL+oalONaYnLKaNwcFlJWv/Nzrabdg8C5IIkD3wKiY299dC/lzuS8LrPBESAiWMni4p7kHOiPr6zJUQg8dql5BZZQ5K2o+yNASURuLiBccWlWW3qqqzXXKR/O38bP+P+LSxJ81NLDEkt6zlARS/skA9rk8Vjz4c2OrrklxPNkO7xP0AgVgChAsrrXHhIVR0IPxt5zSJgHH2udjcXPZsZ7iU5sUiGtxPiEcVtHHgyMXSL7BlmI9rY71t8zHCNAryfgoKDQh1rKFPqR3nTG+4+1JHSxMsPsv1YGIV77TEjtG4VgV7yA7WxZD2KqjecCf6RrY7v0mA1d+/dvg6kRoj1Qgr4Yg3K3yuGxvAlm2zzbZqLMq+UOxvzd", indexDBPassword);

                deleteOrphansCommand.CommandType = CommandType.TableDirect;
                deleteOrphansCommand.CommandText = Helpers.DecryptString("Cm7OW3JhHiTr7x0Ks0/8LA==", indexDBPassword);
                deleteOrphansCommand.IndexName = eventsPrimaryKeyName;

                using (SqlCeResultSet orphanedEventsResults = orphanedEventsCommand.ExecuteResultSet(ResultSetOptions.Scrollable))
                using (SqlCeResultSet deleteOrphanResults = deleteOrphansCommand.ExecuteResultSet(ResultSetOptions.Scrollable | ResultSetOptions.Sensitive | ResultSetOptions.Updatable))
                {
                    foreach (SqlCeUpdatableRecord currentOrphanedEvent in orphanedEventsResults.OfType<SqlCeUpdatableRecord>())
                    {
                        if (deleteOrphanResults.Seek(DbSeekOptions.FirstEqual, currentOrphanedEvent[eventsPrimaryKeyColumn])
                            && deleteOrphanResults.Read())
                        {
                            deleteOrphanResults.Delete();
                        }
                    }
                }
            }

            using (SqlCeCommand removeFileSystemObjectsKey = connection.CreateCommand())
            {
                removeFileSystemObjectsKey.CommandText =
                    Helpers.DecryptString("JOyJDprfb6pCaJjyoaPAAtNHg+d0nEEOXxycEx8n0dC0vD8kp2T/QejI2xPtuCXDVk4xlIec4kx67nNsi0C6bPsU7L6rAahAi1s59qb4RoUS79RWRpezn/ICsy4U0H/scWJQEyX/BAzkaITjTYnxNA==", indexDBPassword) +
                    fileSystemObjectsPrimaryKeyName + "]";

                removeFileSystemObjectsKey.ExecuteNonQuery();
            }

            using (SqlCeCommand expandFileSystemObjectsKey = connection.CreateCommand())
            {
                expandFileSystemObjectsKey.CommandText =
                    Helpers.DecryptString("JOyJDprfb6pCaJjyoaPAAtNHg+d0nEEOXxycEx8n0dC0vD8kp2T/QejI2xPtuCXDVk4xlIec4kx67nNsi0C6bCaMxz5KjswZ/Rfxi/f/fagBknHYv1+c7CtHwFXJVDagzAjfTiDKlVUM892Ob6/i8Q==", indexDBPassword) +
                    fileSystemObjectsPrimaryKeyName +
                    Helpers.DecryptString("/Ue4edUDHSznIIMoz9C9hNoyzBMgZPtOmxuxCo1CFanbjdGkkLH5JieLmeJ/JNChhxqz5XtjDHmQj5xF815M0zhvDHBleQ3Fipb9KVpoQ3+BPkQFVqZ2Tn0o+IInWC/GHv5Dejh0xerYPAM0QcI4fw==", indexDBPassword);

                expandFileSystemObjectsKey.ExecuteNonQuery();
            }
        }
        #endregion
    }
}