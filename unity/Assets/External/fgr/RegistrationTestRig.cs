// ----------------------------------------------------------------------------
// -                       Fast Global Registration                           -
// ----------------------------------------------------------------------------
// The MIT License (MIT)
//
// Copyright (c) Intel Corporation 2016
// Qianyi Zhou <Qianyi.Zhou@gmail.com>
// Jaesik Park <syncle@gmail.com>
// Vladlen Koltun <vkoltun@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
// ----------------------------------------------------------------------------
//
// Adapted to Unity: Shaun Bangay, from main.cpp
//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegistrationTestRig : MonoBehaviour
{
    // For main
    public string feature_01 = "";
    public string feature_02 = "";
    public string transform_output_txt = "";
    
    public bool evaluation = false;
    
    // For evaluation.
    public string transform_gth_log = "";
    public string transform_est_txt = "";
    public string eval_txt = "";
    
    
    void Start()
    {
        if (evaluation)
        {
            if ((feature_01 == "") || (feature_02 == "") || (transform_gth_log == "") || (transform_est_txt == "") || (eval_txt == ""))
            {
                Debug.Log ("Usage ::\n provide [feature_01] [feature_02] [transform_gth_log] [transform_est_txt] [eval_txt]\n");
                return;
            }
        }
        else
        {
            if ((feature_01 == "") || (feature_02 == "") || (transform_output_txt == ""))
            {
                Debug.Log ("Usage ::\n provide [feature_01] [feature_02] [transform_output_txt]\n");
                return;
            }
        }
        
        fgr.CApp app = new fgr.CApp ();
        app.ReadFeature(feature_01);
        app.ReadFeature(feature_02);
        
        if (evaluation)
        {
            app.Evaluation(transform_gth_log, transform_est_txt, eval_txt);
        }
        else
        {
            app.NormalizePoints();
            app.AdvancedMatching();
            app.OptimizePairwise(true);
            app.WriteTrans(transform_output_txt);
        }
        
    }
}
