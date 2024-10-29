using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IMLD.MixedRealityAnalysis.Core;
using System.Data;
using System;

public class MIRIALiteOrchestrator : MonoBehaviour
{
    public string StudyFile;
    public int ConditionFilter;
    public int SessionFilter;

    private int _conditionFilter, _sessionFilter;
    private bool _firstFrame = true;
    // Start is called before the first frame update
    //async void Start()
    //{
    //    if (Services.DataManager() == null || Services.VisManager() == null)
    //    {
    //        Debug.LogError("MIRIA components missing!");
    //        return;
    //    }

    //    // load study data
    //    await Services.DataManager().LoadStudyAsync(StudyFile);

    //    // select data sets
    //    List<int> dataSets = new List<int>();
    //    for (int i = 0; i < Services.DataManager().DataSets.Count; i++)
    //    {
    //        if (Services.DataManager().DataSets[i].ObjectType != ObjectType.TOUCH)
    //        {
    //            dataSets.Add(i);
    //        }
    //    }

    //    // select conditions
    //    var conditions = new List<int>();
    //    if (ConditionFilter < 0)
    //    {
    //        for (int i = 0; i < Services.DataManager().CurrentStudy.Conditions.Count; i++)
    //        {
    //            conditions.Add(i);
    //        }
    //    }
    //    else
    //    {
    //        conditions.Add(ConditionFilter);
    //    }

    //    // select sessions
    //    var sessions = new List<int>();
    //    if (SessionFilter < 0)
    //    {
    //        for (int i = 0; i < Services.DataManager().CurrentStudy.Sessions.Count; i++)
    //        {
    //            sessions.Add(i);
    //        }
    //    }
    //    else
    //    {
    //        sessions.Add(SessionFilter);
    //    }

    //    // create test visualization
    //    Services.VisManager().CreateVisualization(
    //        new VisProperties(
    //            Guid.Empty,
    //            VisType.Trajectory3D,
    //            -1,
    //            dataSets,
    //            conditions,
    //            sessions));

    //    _conditionFilter = ConditionFilter;
    //    _sessionFilter = SessionFilter;
    //}
    private void Start()
    {
        //Services.StudyManager().LoadStudy(0);
    }

    // Update is called once per frame
    async void Update()
    {
        if(_firstFrame)
        {
            _firstFrame = false;
            await Services.StudyManager().LoadStudy(0);

            // select data sets
            List<int> dataSets = new List<int>();
            for (int i = 0; i < Services.DataManager().DataSets.Count; i++)
            {
                if (Services.DataManager().DataSets[i].ObjectType != ObjectType.TOUCH)
                {
                    dataSets.Add(i);
                }
            }

            // select conditions
            var conditions = new List<int>();
            if (ConditionFilter < 0)
            {
                for (int i = 0; i < Services.DataManager().CurrentStudy.Conditions.Count; i++)
                {
                    conditions.Add(i);
                }
            }
            else
            {
                conditions.Add(ConditionFilter);
            }

            // select sessions
            var sessions = new List<int>();
            if (SessionFilter < 0)
            {
                for (int i = 0; i < Services.DataManager().CurrentStudy.Sessions.Count; i++)
                {
                    sessions.Add(i);
                }
            }
            else
            {
                sessions.Add(SessionFilter);
            }

            // create test visualization
            Services.VisManager().CreateVisualization(
                new VisProperties(
                    Guid.Empty,
                    VisType.Trajectory3D,
                    -1,
                    dataSets,
                    conditions,
                    sessions));

            _conditionFilter = ConditionFilter;
            _sessionFilter = SessionFilter;
        }


        if (_conditionFilter != ConditionFilter || _sessionFilter != SessionFilter)
        {
            // select conditions
            var conditions = new List<int>();
            if (ConditionFilter < 0)
            {
                for (int i = 0; i < Services.DataManager().CurrentStudy.Conditions.Count; i++)
                {
                    conditions.Add(i);
                }
            }
            else
            {
                conditions.Add(ConditionFilter);
            }

            // select sessions
            var sessions = new List<int>();
            if (SessionFilter < 0)
            {
                for (int i = 0; i < Services.DataManager().CurrentStudy.Sessions.Count; i++)
                {
                    sessions.Add(i);
                }
            }
            else
            {
                sessions.Add(SessionFilter);
            }

            Services.VisManager().UpdateSessionFilter(sessions, conditions);

            _conditionFilter = ConditionFilter;
            _sessionFilter = SessionFilter;
        }
    }
}
