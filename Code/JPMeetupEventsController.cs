using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using JobPlacementDashboard.Models;
using Microsoft.AspNet.Identity;

namespace JobPlacementDashboard.Controllers
{
    public class JPMeetupEventsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: JPMeetupEvents
        public ActionResult Index()
        {
            return View();
        }

        public PartialViewResult _MeetUpApi()
        {
            DateTime now = DateTime.Now;
            DateTime recently = now.AddHours(-2);
            var updateCheck = db.JPMeetupEvents.Where(x => x.JPDateCreated >= recently);
            if (updateCheck.Count() == 0) // If no events have been added in the last two hours, pull from API.  This throttles how often we request API to prevent lockout.
            {
                string[] meetupRequestUrls = {
                //Portland Meetups//
                @"https://api.meetup.com/techacademy/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/AgilePDX-User-Group-Portland-Metro/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/DesignOps-Portland/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Google-Development-Group-GDG-PDX-Meetup/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/IxDA-Portland/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Pacific-NW-Software-Quality-Conference-PNSQC/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Placemaking-in-the-Digital-Age/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Portland-Drupal/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Portland-ReactJS/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/PSU-Distinguished-Speaker-Series-at-the-Dept-of-ETM/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/alchemycodelab/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Figma-Portland/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/GraphQLPDX/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/JAMstack-Portland/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/New-Relic-FutureTalks-PDX/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/PDX-PHP/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Portland-Accessibility-and-User-Experience-Meetup/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Portland-Programmer-Network/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Portland-Tech-Leadership-Meetup/events?photo-host=public&page=20&page=20",
                //Seattle Meetups//
                @"https://api.meetup.com/seattle-api/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/seattlejs/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Seattle-WebDev-Coffee/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/seattle-react-js/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Beer-Code-Seattle/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/She-Codes-Now-Seattle/events?photo-host=public&page=20&page=20",
                @"https://api.meetup.com/Seattle-scalability-meetup/events?photo-host=public&page=20&page=20",
                //Denver Meetups//
                @"https://api.meetup.com/Denver-Engineering-Leaders/events?&sign=true&photo-host=public&page=20",
                @"https://api.meetup.com/UX-Bookclub-Denver/events?&sign=true&photo-host=public&page=20",
                @"https://api.meetup.com/ReactDenver/events?&sign=true&photo-host=public&page=20",
                @"https://api.meetup.com/DenverUX/events?&sign=true&photo-host=public&page=20",
                @"https://api.meetup.com/colorado-diversity-in-tech/events?&sign=true&photo-host=public&page=20",
                @"https://api.meetup.com/Denver-IASA/events?&sign=true&photo-host=public&page=20",
                @"https://api.meetup.com/Denver-Tech-Design-Community/events?&sign=true&photo-host=public&page=20",
                @"https://api.meetup.com/Develop-Denver/events?&sign=true&photo-host=public&page=20",
                @"https://api.meetup.com/Women-Who-Code-Boulder-Denver/events?photo-host=public&page=20",
                @"https://api.meetup.com/Ladies-that-UX-Denver/events?photo-host=public&page=20",
                @"https://api.meetup.com/Denver-Python-Meetup/events?photo-host=public&page=20",
                @"https://api.meetup.com/Denver-Mock-Programming-Job-Meetup/events?photo-host=public&page=20",
                @"https://api.meetup.com/data-science-women/events?photo-host=public&page=20",
                };

                var events = new List<JPMeetupEvent>();
                try
                {
                    var responseStrings = new List<string>();
                    foreach (var url in meetupRequestUrls)
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            responseStrings.Add(reader.ReadToEnd());
                        }
                    }

                    foreach (var jsonString in responseStrings)  // This loop takes the Json information in the strings, converts them into dynamic objects and extracts meetup info from those objects.
                    {
                        dynamic meetupDynamic = System.Web.Helpers.Json.Decode(jsonString);  // Decodes JSON string into a dynamic object that automatically generates properties.
                        int jsonObjectCount = meetupDynamic.Length;
                        for (int i = 0; i < jsonObjectCount; i++)  // This loop gets the JPMeetupEvent data from the dynamic class.
                        {
                            JPMeetupEvent jpEvent = new JPMeetupEvent()
                            {
                                JPEventName = meetupDynamic[i].name,  // The .name, .link, etc are the property names assigned by the decoder based on the JSON API string.
                                JPEventLink = meetupDynamic[i].link,
                                JPEventDate = Convert.ToDateTime(meetupDynamic[i].local_date + " " + meetupDynamic[i].local_time),
                                JPDateCreated = now
                            };

                            try  // If no city has been selected for the event, no venue.city property is created and an error is thrown.
                            {
                                jpEvent.JPLocation = meetupDynamic[i].venue.city;
                            }
                            catch  // Get city from API group info instead.
                            {
                                string city = meetupDynamic[i].group.localized_location; // This comes in City, ST format and needs parsing.
                                city = city.Substring(0, city.LastIndexOf(","));
                                jpEvent.JPLocation = city;
                            }

                            events.Add(jpEvent);
                        };

                    }

                    //remove old events from table
                    db.JPMeetupEvents.RemoveRange(db.JPMeetupEvents);
                    db.JPMeetupEvents.AddRange(events);

                    db.SaveChanges();

                    //Filter events for current user
                    events = FilterEvents(events);
                }

                //if there is a web exception, load events from the database instead
                //this try/catch block may no longer be necessary because we're controlling API pull frequency in the if/else.
                catch (WebException)
                {
                    events = GetStoredEvents();
                    events = FilterEvents(events);
                }

                return PartialView("_MeetUpApi", events);
            }
            else // If events have been pulled in the last 2 hours, use those events
            {
                var events = GetStoredEvents();
                events = FilterEvents(events);
                return PartialView("_MeetUpApi", events);
            }
        }

        private List<JPMeetupEvent> FilterEvents(List<JPMeetupEvent> events)
        {
            events = FilterDuplicateEvents(events);
            events = FilterLocationEvents(events);
            events = SortDates(events);
            return events;
        }

        private List<JPMeetupEvent> GetStoredEvents()
        {
            List<JPMeetupEvent> events = new List<JPMeetupEvent>();
            foreach (var meetupEvent in db.JPMeetupEvents)
            {
                events.Add(meetupEvent);
            };
            return events;
        }

        public List<JPMeetupEvent> FilterPastEvents(List<JPMeetupEvent> events)
        {
            var filteredList = new List<JPMeetupEvent>();

            foreach (var meetupEvent in events)
            {
                if (meetupEvent.JPEventDate.CompareTo(DateTime.Now) >= 0)
                {
                    filteredList.Add(meetupEvent);
                }
            }

            return filteredList;
        }

        //will keep the earliest of the duplicates
        List<JPMeetupEvent> FilterDuplicateEvents(List<JPMeetupEvent> events)
        {
            var filteredEvents = new List<JPMeetupEvent>();
            var eventNames = new List<string>();
            foreach (var meetupEvent in events)
            {
                if (!eventNames.Contains(meetupEvent.JPEventName))
                {
                    eventNames.Add(meetupEvent.JPEventName);
                    filteredEvents.Add(meetupEvent);
                }
            }
            return filteredEvents;
        }


        //Sort event dates
        List<JPMeetupEvent> SortDates(List<JPMeetupEvent> events)
        {
            var filteredList = new List<JPMeetupEvent>(events.OrderBy(x => x.JPEventDate));
            return filteredList;
        }


        //filter events by location
        //Modify the logic in the JPBulletins controller that has to do with the 
        //    events pulled from Meetups API to filter the meetups that are shown 
        //    to a user depending on whether they are Seattle(local or remote) 
        //    vs Portland(local or remote)
        List<JPMeetupEvent> FilterLocationEvents(List<JPMeetupEvent> events)
        {
            // Determine Location of User
            var UserId = User.Identity.GetUserId();
            var StudentLocation = db.JPStudents.Where(x => x.ApplicationUserId == UserId).Select(x => x.JPStudentLocation).FirstOrDefault().ToString();

            if (StudentLocation == "PortlandLocal" || StudentLocation == "PortlandRemote")
            {
                StudentLocation = "Portland";
            }
            else if (StudentLocation == "SeattleLocal" || StudentLocation == "SeattleRemote")
            {
                StudentLocation = "Seattle";
            }
            else if (StudentLocation == "DenverLocal" || StudentLocation == "DenverRemote")
            {
                StudentLocation = "Denver";
            }
            else
            {
                StudentLocation = "Portland";  // Setting a default of Portland if no location comes through.
            }


            // Filter event location by user location
            var filteredEvents = new List<JPMeetupEvent>();

            foreach (var meetup in events)
            {
                if (meetup.JPLocation == StudentLocation)
                {
                    filteredEvents.Add(meetup);
                }
            }
            return filteredEvents;
        }
    }
}