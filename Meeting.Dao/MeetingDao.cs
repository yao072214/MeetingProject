﻿using Meeting.Common;
using Meeting.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Meeting.Dao
{
    public class MeetingDao
    {
        public static DataSet GetMeetingList(int meetingType, int pageindex, int pagesize)
        {
            int index = (pageindex - 1) * pagesize + 1;
            int size = pageindex * pagesize;

            DataSet dataSet = new DataSet();

            string sql = @"select RowId,MeetingId,MeetingName,StartDate,EendDate,MeetingType
                                  from ( select ROW_NUMBER() OVER (ORDER BY StartDate desc) RowId,MeetingId,
                                  MeetingName,StartDate,EendDate,MeetingType from m_Meeting 
                                  where MeetingType=@meetingType ) a where a.RowId between @index and @size;
                                  select count(1) from m_Meeting where MeetingType=@meetingType";



            SqlParameter[] paras = new SqlParameter[]
           {
               new SqlParameter("@index",index),
               new SqlParameter("@size",size),
               new SqlParameter("@meetingType",meetingType)
           };

            dataSet = SQLHelper.GetDataSet(sql, paras);

            return dataSet;
        }


        public static mMeeting GetMeetingModel(int meetingId) 
        {
            mMeeting model = new mMeeting();
            model.IssueList=new mMeetingIssue();
            //mMeetingIssue issue = null;

            string sql = @"select m.MeetingId,MeetingName,StartDate,EendDate,MeetingAddress,
                                   HostName=(select UserName from m_User u where u.UserId=m.MeetingHost),
                                   SecretaryName=(select UserName from m_User u where u.UserId=m.MeetingSecretary),
                                   peopleName=(select [dbo].[GetMeetingPeople](1)),mi.IssueName,mi.Id,
                                   RepostUser=(select UserName from m_User u where u.UserId=mi.RepostUser),
                                   DepartName=(select DepartName from m_Depart m where m.Id=mi.DepartId)
                                   from m_Meeting m  left join m_MeetingIssue mi on m.MeetingId=mi.MeetingId
                                   left join m_Address a on m.AddressId=a.Id
                                   where m.MeetingId=@meetingId";

            SqlParameter[] paras = new SqlParameter[]
           {
               new SqlParameter("@meetingId",meetingId),
           };

            SqlDataReader reader = SQLHelper.GetReader(sql,paras);
            while (reader.Read()) 
            {

                model.IssueList.IssueName = reader["IssueName"].ToString();
                model.IssueList.RepostUser = reader["RepostUser"].ToString();
                model.IssueList.DepartName = reader["DepartName"].ToString();
                model.IssueList.Id = Tool.ToInt(reader["Id"].ToString());


                model.MeetingId = Tool.ToInt(reader["MeetingId"].ToString());
                model.MeetingName = reader["MeetingName"].ToString();
                model.StartDate =Convert.ToDateTime(reader["StartDate"]).ToString("yyyy-MM-dd HH:mm");
                model.EendDate = Convert.ToDateTime(reader["EendDate"]).ToString("yyyy-MM-dd HH:mm");
                model.MeetingAddress = reader["MeetingAddress"].ToString();
                model.MeetingHost = reader["HostName"].ToString();
                model.SecretaryName = reader["SecretaryName"].ToString();
                model.PeopleName = reader["PeopleName"].ToString();
            }

            return model;
        }


        public static DataSet GetCreateMeeting() 
        {
            DataSet dataSet = new DataSet();

            string usersql = "select UserId,UserName from [dbo].[m_User];";
            string addresssql = "select Id,MeetingAddress from [dbo].[m_Address];";
            string departsql="select Id,DepartName from [dbo].[m_Depart];";

            usersql = usersql + addresssql + departsql;

            dataSet = SQLHelper.GetDataSet(usersql);
            return dataSet;
        }


        public static int SaveMeeting(List<mMeetingResources> resources, List<mMeetingPeople> people,mMeeting meeting)
        {
            int result = 0;

            string meetingsql = string.Format(@"insert into [m_Meeting](MeetingName,StartDate,EendDate,AddressId,
                                   MeetingHost,MeetingDocument,MeetingCreateDate,MeetingType,
                                   MeetingSecretary) values('{0}','{1}',
                                   '{2}','{3}','{4}','{5}','{6}','{7}','{8}');select @@identity", meeting.MeetingName, meeting.StartDate,
                                    meeting.EendDate,meeting.MeetingAddress,meeting.MeetingHost,meeting.MeetingDocument,
                                    DateTime.Now.ToString(),
                                    0,meeting.MeetingSecretary);

            int meetingid = SQLHelper.ExcuteScalarSQL(meetingsql);

            if (meetingid > 0) 
            {
                string issuesql = string.Format(@"insert into [m_MeetingIssue](IssueName,RepostUser,DepartId,
                                          MeetingId) values('{0}','{1}','{2}','{3}');select @@identity", meeting.IssueList.IssueName,
                                          meeting.IssueList.RepostUserId, meeting.IssueList.DepartId, meetingid);
                StringBuilder builder = new StringBuilder();
                foreach (var item in people)
                {
                    builder.AppendFormat("insert into [m_MeetingPeople] (MeetingId,UserId,RoleId) values('{0}','{1}','{2}');",meetingid,item.UserId,item.RoleId);
                }

                int issueId = SQLHelper.ExcuteScalarSQL(builder.ToString()+issuesql);

                if (issueId > 0) 
                {
                    builder.Clear();
                    foreach (var item in resources)
                    {
                        builder.AppendFormat(@"insert into [m_MeetingResources] (ResourcesName,
                        ResourcesType,MeetingIssueId) values ('{0}','{1}','{2}');",item.ResourcesName,item.ResourcesType,issueId);
                    }

                    result = SQLHelper.ExcuteSQL(builder.ToString());
                }
            }

            return result;
        }
    }
}
