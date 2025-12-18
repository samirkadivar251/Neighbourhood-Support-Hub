using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;

namespace NSH.Models
{
    public class EventService
    {
        private readonly string connectionString;

        public EventService()
        {
            connectionString = "Server=(localdb)\\ProjectModels;Database=EventManagementDB;Integrated Security=True;";
        }

        // Method to get all events
        public List<EventModel> GetAllEvents()
        {
            List<EventModel> events = new List<EventModel>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, Title, Date, Time, Description, Organizer, Location, ImagePath FROM Events";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    events.Add(new EventModel
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        EventTitle = reader["Title"].ToString(),
                        EventDate = Convert.ToDateTime(reader["Date"]),
                        EventTime = reader["Time"].ToString(),
                        EventDescription = reader["Description"].ToString(),
                        OrganizerInfo = reader["Organizer"].ToString(),
                        Location = reader["Location"].ToString(),
                        ImagePath = reader["ImagePath"].ToString()
                    });
                }
            }
            return events;
        }

        // Method to get a specific event by ID
        public EventModel GetEventById(int id)
        {
            EventModel eventModel = null;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, Title, Date, Time, Description, Organizer, Location, ImagePath FROM Events WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    eventModel = new EventModel
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        EventTitle = reader["Title"].ToString(),
                        EventDate = Convert.ToDateTime(reader["Date"]),
                        EventTime = reader["Time"].ToString(),
                        EventDescription = reader["Description"].ToString(),
                        OrganizerInfo = reader["Organizer"].ToString(),
                        Location = reader["Location"].ToString(),
                        ImagePath = reader["ImagePath"].ToString()
                    };
                }
            }
            return eventModel;
        }
    }
}
