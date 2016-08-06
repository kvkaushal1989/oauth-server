﻿using Promact.Oauth.Server.Data_Repository;
using Promact.Oauth.Server.Models.ApplicationClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

using Promact.Oauth.Server.Models;
using Microsoft.AspNetCore.Http;

namespace Promact.Oauth.Server.Repository.ProjectsRepository
{
    public class ProjectRepository : IProjectRepository
    {
        private IDataRepository<Project> _projectDataRepository;
        private IDataRepository<ProjectUser> _projectUserDataRepository;
        private IDataRepository<ApplicationUser> _userDataRepository;
        public ProjectRepository(IDataRepository<Project> projectDataRepository, IDataRepository<ProjectUser> projectUserDataRepository, IDataRepository<ApplicationUser> userDataRepository)
        {
            _projectDataRepository = projectDataRepository;
            _projectUserDataRepository = projectUserDataRepository;
            _userDataRepository = userDataRepository;
        }
        /// <summary>
        /// Get All Projects list from the database
        /// </summary>
        /// <returns></returns>List of Projects
        public IEnumerable<ProjectAc> GetAllProjects()
        {
            var projects = _projectDataRepository.List().ToList();
            var projectAcs = new List<ProjectAc>();
           
            projects.ForEach(project =>
            {
                var TeamLeaderNm = new UserAc();
                TeamLeaderNm.FirstName =_userDataRepository.FirstOrDefault(x => x.Id == project.TeamLeaderId)?.FirstName;
                var CreatedBy = _userDataRepository.FirstOrDefault(x => x.Id == project.CreatedBy)?.FirstName;
                var UpdatedBy = _userDataRepository.FirstOrDefault(x => x.Id == project.UpdatedBy)?.FirstName;
                string UpdatedDate;
                if (project.UpdatedDateTime.ToLocalTime().ToString("dd'/'MM'/'yyyy HH:mm:ss") == "01/01/0001 05:30:00")
                {
                    UpdatedDate = "";
                }
                else
                {
                    UpdatedDate = project.UpdatedDateTime.ToLocalTime().ToString("dd'/'MM'/'yyyy HH:mm");
                }
                projectAcs.Add(new ProjectAc
                {
                    Id = project.Id,
                    Name = project.Name,
                    IsActive = project.IsActive,
                    SlackChannelName = project.SlackChannelName,
                    TeamLeaderId = project.TeamLeaderId,
                    TeamLeader = TeamLeaderNm,
                    CreatedBy= CreatedBy,
                    CreatedDate=project.CreatedDateTime.ToLocalTime().ToString("dd'/'MM'/'yyyy HH:mm"),
                    UpdatedBy=UpdatedBy,//01/01/0001 05:30:00
                    UpdatedDate= UpdatedDate
                });
                
            });
            return projectAcs;
        }
    
        /// <summary>
        /// Adds new project in the database
        /// </summary>
        /// <param name="newProject">project that need to be added</param>
        /// <param name="createdBy">Login User Id</param>
        /// <returns>project id of newly created project</returns>
        public int AddProject(ProjectAc newProject,string createdBy)
        {
            
            try
            {
                Project project = new Project();
                project.IsActive = newProject.IsActive;
                project.Name = newProject.Name;
                project.TeamLeaderId = newProject.TeamLeaderId;
                project.SlackChannelName = newProject.SlackChannelName;
                project.CreatedDateTime = DateTime.UtcNow;
                project.CreatedBy = createdBy;
                _projectDataRepository.Add(project);
                return project.Id;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// Adds UserId and ProjectId in UserProject table
        /// </summary>
        /// <param name="newProjectUser"></param>ProjectId and UserId information that need to be added
        public void AddUserProject(ProjectUser newProjectUser)
        {
            _projectUserDataRepository.Add(newProjectUser);
        }

        /// <summary>
        /// Get the single project and list of users related project Id from the database(project and ProjectUser Table)
        /// </summary>
        /// <param name="id"></param>Project id that need to be featch the Project and list of users
        /// <returns></returns>Project and User/Users infromation 
        /// 
        public ProjectAc GetById(int id)
        {

            List<UserAc> applicationUserList = new List<UserAc>();
            var project = _projectDataRepository.FirstOrDefault(x => x.Id == id);
            List<ProjectUser> projectUserList = _projectUserDataRepository.Fetch(y => y.ProjectId == project.Id).ToList();
            foreach (ProjectUser pu in projectUserList)
            {
                var applicationUser = _userDataRepository.FirstOrDefault(z => z.Id == pu.UserId);
                applicationUserList.Add(new UserAc
                {
                    Id = applicationUser.Id,
                    FirstName = applicationUser.FirstName
                });
            }
            var projectAc = new ProjectAc();
            projectAc.Id = project.Id;
            projectAc.SlackChannelName = project.SlackChannelName;
            projectAc.IsActive = project.IsActive;
            projectAc.Name = project.Name;
            projectAc.TeamLeader = new UserAc();
            projectAc.TeamLeaderId = project.TeamLeaderId;
            projectAc.TeamLeader.FirstName = _userDataRepository.FirstOrDefault(x => x.Id == project.TeamLeaderId)?.FirstName;
            projectAc.ApplicationUsers = applicationUserList;
            return projectAc;
        }
      
        /// <summary>
        /// Update Project information and User list information In Project table and Project User Table
        /// </summary>
        /// <param name="editProject"></param>Updated information in editProject Parmeter
        /// <param name="updatedBy"></param>Login User Id
        public void EditProject(ProjectAc editProject,string updatedBy)
        {
            var projectId = editProject.Id;
            var projectInDb = _projectDataRepository.FirstOrDefault(x => x.Id == projectId);
            projectInDb.IsActive = editProject.IsActive;
            projectInDb.Name = editProject.Name;
            projectInDb.TeamLeaderId = editProject.TeamLeaderId;
            projectInDb.SlackChannelName = editProject.SlackChannelName;
            projectInDb.UpdatedDateTime = DateTime.UtcNow;
            projectInDb.UpdatedBy = updatedBy;
            _projectDataRepository.Update(projectInDb);

            var CreatedBy = _userDataRepository.FirstOrDefault(x => x.Id == editProject.CreatedBy)?.FirstName;
            //Delete old users from project user table
            _projectUserDataRepository.Delete(x => x.ProjectId == projectId);
            _projectUserDataRepository.Save();

            editProject.ApplicationUsers.ForEach(x =>
            {
                _projectUserDataRepository.Add(new ProjectUser
                {
                    ProjectId = projectInDb.Id,
                    UpdatedDateTime = DateTime.UtcNow,
                    UpdatedBy = updatedBy,
                    CreatedBy = projectInDb.CreatedBy,
                    CreatedDateTime = projectInDb.CreatedDateTime,
                    UserId=x.Id
                });
            });
        }
    }
}