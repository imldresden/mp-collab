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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.Statistics;
using UnityEngine;
using KdTree;
using KdTree.Math;

namespace fgr
{
    public class Points : List <Vector> {} // of 3 element vectors.
    public class Feature : List <Vector> {} // of X element vectors.
    public class Correspondences : List <(int, int)> {} 
    
    public class CApp
    {
        public const double DIV_FACTOR = 1.4;		// Division factor used for graduated non-convexity
        public const bool USE_ABSOLUTE_SCALE = false;		// Measure distance in absolute scale (1) or in scale relative to the diameter of the model (0)
        public const double MAX_CORR_DIST = 0.25;	// Maximum correspondence distance (also see comment of USE_ABSOLUTE_SCALE)
        public const int ITERATION_NUMBER = 64;		// Maximum number of iteration
        public const float TUPLE_SCALE = 0.95f;	// Similarity measure used for tuples of feature points. (tau)
        public const int TUPLE_MAX_CNT = 1000;	// Maximum tuple numbers.
        
        // containers
        private List <Points> pointcloud_;
        private List <Feature> features_;
        private Matrix4x4 TransOutput_;
        private List <(int, int)> corres_;

        // for normalization
        private Points Means;
        private float GlobalScale = 1.0f;
        private float StartScale = 1.0f;

        private double div_factor_;
        private bool   use_absolute_scale_;
        private double max_corr_dist_;
        private int    iteration_number_;
        private float  tuple_scale_;
        private int    tuple_max_cnt_;    
        private float searchRadius; // r
        private int maxNeighbours; // k
        
        public CApp ()
        {
            div_factor_         = DIV_FACTOR;
	        use_absolute_scale_ = USE_ABSOLUTE_SCALE;
	        max_corr_dist_      = MAX_CORR_DIST;
	        iteration_number_   = ITERATION_NUMBER;
	        tuple_scale_        = TUPLE_SCALE;
	        tuple_max_cnt_      = TUPLE_MAX_CNT;
            
            searchRadius = 0.85f;
            //maxNeighbours = 80;
            maxNeighbours = 20;

            pointcloud_ = new List <Points> ();
            features_ = new List <Feature> ();
            
            Means = new Points ();
        }
        
        public void ReadFeature(string filepath)
        {
            Points pts = new Points ();
            Feature feat = new Feature ();
            ReadFeature(filepath, pts, feat);
            LoadFeature(pts,feat);
        }
        
        public void LoadFeature(Points pts, Feature feat)
        {
            pointcloud_.Add(pts);
            features_.Add(feat);
        }        
        
        // Adapted to deal with normals that might face inwards or outwards.
        // Try both directions, and select largest value (as presumably better to discriminate).
        private void computeAngles (Vector ps, Vector ns, Vector pt, Vector nt, out float alpha, out float phi, out float theta)
        {
            Vector3 psv = new Vector3 (ps[0], ps[1], ps[2]);
            Vector3 ptv = new Vector3 (pt[0], pt[1], pt[2]);
            Vector3 ntv = new Vector3 (nt[0], nt[1], nt[2]).normalized;
            Vector3 ntvalt = -ntv;
            Vector3 u = new Vector3 (ns[0], ns[1], ns[2]).normalized;
            Vector3 ualt = -u;
            Vector3 v = Vector3.Cross (u, (ptv - psv).normalized);
            Vector3 valt = Vector3.Cross (-u, (ptv - psv).normalized);
            Vector3 w = Vector3.Cross (u, v);
            Vector3 walt = Vector3.Cross (-u, v);
            float d = (ptv - psv).magnitude;
            alpha = Mathf.Max (Vector3.Dot (v, ntv), Vector3.Dot (v, ntvalt), Vector3.Dot (valt, ntv), Vector3.Dot (valt, ntvalt));
            phi = Mathf.Max (Vector3.Dot (u, (ptv - psv) / d), Vector3.Dot (ualt, (ptv - psv) / d));
//             Debug.Log ("D " + d + " " + u + " " + new Vector3 (ns[0], ns[1], ns[2]));
            float thetad = Mathf.Max (Vector3.Dot (w, ntv), Vector3.Dot (w, ntvalt), Vector3.Dot (walt, ntv), Vector3.Dot (walt, ntvalt));
            float thetan = Mathf.Max (Vector3.Dot (u, ntv), Vector3.Dot (u, ntvalt), Vector3.Dot (ualt, ntv), Vector3.Dot (ualt, ntvalt));
            theta = Mathf.Atan2 (thetad, thetan);
        }

        private Points findNormals (Points pts, List <List<int>> neighbourCorres, List <List<float>> neighbourDis)
        {

            Points normals = new Points ();
            
            for (int i = 0; i < pts.Count; i++)
            {
                List <Vector> p = new List <Vector> ();
                for (int j = 0; j < neighbourCorres[i].Count; j++)
                {
                  p.Add (pts[neighbourCorres[i][j]]);
                }
                
                if (p.Count == 0)
                {
                    // no neighbours. Fudge normal.
                    normals.Add (pts[i]);
                }
                else
                {
                    // Fit: (pi - x).n = 0
                    // with pi is point on plane. Use pts[i]. x is current point, p[j]
                    Matrix M = new DenseMatrix (p.Count, 3);
                    for (int j = 0; j < p.Count; j++)
                    {
                        M[j, 0] = pts[i][0] - p[j][0];
                        M[j, 1] = pts[i][1] - p[j][1];
                        M[j, 2] = pts[i][2] - p[j][2];
                    }
                    
                    MathNet.Numerics.LinearAlgebra.Factorization.Svd<float> svdResult = M.Svd ();
                    Matrix Vt = (Matrix) svdResult.VT;
            
                    // Last row of Vt has the desired solution.
                    Vector pp = (Vector) Vt.Row (Vt.RowCount - 1);
                    
                    normals.Add (pp);
                }
            }
            return normals;            
        }
        
        private Feature calculateSPFH (Points pts, Points normals, List <List<int>> neighbourCorres, List <List<float>> neighbourDis)
        {
            Feature spfh = new Feature ();
            float alpha, phi, theta;
            for (int i = 0; i < pts.Count; i++)
            {
                List <double> alphas = new List <double> ();
                List <double> phis = new List <double> ();
                List <double> thetas = new List <double> ();
                for (int j = 0; j < neighbourCorres[i].Count; j++)
                {
                    computeAngles (pts[i], normals[i], pts[neighbourCorres[i][j]], normals[neighbourCorres[i][j]], out alpha, out phi, out theta); 
                    alphas.Add (alpha);
                    phis.Add (phi);
                    thetas.Add (theta);
                }
                Vector feat_v = new DenseVector (33);
                if (alphas.Count > 0)
                {
                    Histogram alphah = new Histogram (alphas, 11);
                    Histogram phih = new Histogram (phis, 11);
                    Histogram thetah = new Histogram (thetas, 11);
                    
                    for (int j = 0; j < 11; j++)
                    {
                        feat_v[j * 3 + 0] = ((float) alphah[j].Count) / alphah.BucketCount;
                        feat_v[j * 3 + 1] = ((float) phih[j].Count) / phih.BucketCount;
                        feat_v[j * 3 + 2] = ((float) thetah[j].Count) / thetah.BucketCount;
                    }
                }
                else
                {
                    for (int j = 0; j < 33; j++)
                    {
                        feat_v[j] = 0.0f;
                    }
                }
                
                spfh.Add (feat_v);
            }
            return spfh;
        }
        
        // Fast point feature histograms: https://pcl.readthedocs.io/projects/tutorials/en/latest/fpfh_estimation.html
        private Feature calculateFPFH (Points pts, Points normals, Feature spfh, List <List<int>> neighbourCorres, List <List<float>> neighbourDis)
        {
            Feature fpfh = new Feature ();
            for (int i = 0; i < pts.Count; i++)
            {
                Vector feat_v = spfh[i];

                int k = neighbourCorres[i].Count;
                
                for (int j = 0; j < neighbourCorres[i].Count; j++)
                {
                    feat_v = (Vector) (feat_v + spfh[neighbourCorres[i][j]] / (k * neighbourDis[i][j]));
                }
                
                fpfh.Add (feat_v);
            }
            
            return fpfh;
        }
        
        public void AddFeature(List <Vector3> pointCloud)
        {
            Points pts = new Points ();

            for (int v = 0; v < pointCloud.Count; v++)	{
                Vector pts_v = new DenseVector (new float [] { pointCloud[v].x, pointCloud[v].y, pointCloud[v].z });
                pts.Add(pts_v);
            }

            // Find the neighbours for each point, limited by radius and maximum number.
            // This information is used in several steps, so get this once and reuse.
            KdTree<float, int> tree = new KdTree<float, int> (3, new FloatMath());
            BuildKDTree(pts, tree);
            List <List<int>> neighbourCorres = new List <List<int>> ();
            List <List<float>> neighbourDis = new List <List<float>> ();
            
            // Find neighbours.
            for (int i = 0; i < pts.Count; i++)
            {
                List<int> corres = new List<int> ();
                List<float> dis = new List<float> ();
            
                SearchKDTree(tree, pts[i], corres, dis, maxNeighbours);

                List<int> validCorres = new List<int> ();
                List<float> validDis = new List<float> ();
                List <Vector> p = new List <Vector> ();
                for (int j = 0; j < corres.Count; j++)
                {
                  if ((corres[j] != i) && (dis[j] < searchRadius) && (dis[j] > 0.0f))
                  {
                      validCorres.Add (corres[j]);
                      validDis.Add (dis[j]);
                  }
                }
                neighbourCorres.Add (validCorres);
                neighbourDis.Add (validDis);
            }
            
            Points normals = findNormals (pts, neighbourCorres, neighbourDis);
            Feature spfh = calculateSPFH (pts, normals, neighbourCorres, neighbourDis);
            Feature fpfh = calculateFPFH (pts, normals, spfh, neighbourCorres, neighbourDis);
            
            int ndim = fpfh[0].Count;
//             for (int v = 0; v < pointCloud.Count; v++)	{
//                 float [] vals = new float [ndim];
//                 vals[0] = 1.0f;
//                 Vector feat_v = new DenseVector (vals);
//                 feat.Add(feat_v);
//             }
            Debug.LogFormat("{0} points with {1} feature dimensions.\n", pointCloud.Count, ndim);
            LoadFeature(pts, fpfh);
        }
        
        private void ReadFeature(string filepath, Points pts, Feature feat)
        {
            Debug.Log ("ReadFeature ... ");
            FileStream fid = File.Open (filepath, FileMode.Open);
            BinaryReader reader = new BinaryReader (fid, Encoding.UTF8, false);
            int nvertex;
            nvertex = reader.ReadInt32 ();
            
            int ndim;
            ndim = reader.ReadInt32 ();
            
            // read from feature file and fill out pts and feat
            for (int v = 0; v < nvertex; v++)	{
                
                float [] vals;
                
                vals = new float [3];
                for (int i = 0; i < 3; i++)
                {
                  vals[i] = reader.ReadSingle ();
                }
                Vector pts_v = new DenseVector (vals);
                
                vals = new float [ndim];
                for (int i = 0; i < ndim; i++)
                {
                  vals[i] = reader.ReadSingle ();
                }
                Vector feat_v = new DenseVector (vals);

                pts.Add(pts_v);
                feat.Add(feat_v);
            }
            fid.Close ();
            
            Debug.LogFormat("{0} points with {1} feature dimensions.\n", nvertex, ndim);

        }
        
        private float sqrnorm (Vector a)
        {
            return (float) (a.Norm (2.0) * a.Norm (2.0));
        }
        
        private float norm (Vector a)
        {
            return (float) (a.Norm (2.0));
        }
        
        private void BuildKDTree (List <Vector> data, KdTree<float, int> tree)
        {
            int rows, dim;
            rows = (int)data.Count;
            dim = (int)data[0].Count;
            for (int i = 0; i < rows; i++)
            {
                float [] datarow = new float [dim];
                for (int j = 0; j < dim; j++)
                {
                    datarow[j] = data[i][j];
                }
                tree.Add (datarow, i);
            }
        }
        
        private void SearchKDTree (KdTree<float, int> tree, Vector input, 
                                List <int> indices,
                                List <float> dists, int nn)
        {
            int rows_t = 1;
            int dim = input.Count;
            
            float [] datarow = new float [dim];
            for (int j = 0; j < dim; j++)
            {
                datarow[j] = input[j];
            }
            Vector datarowv = new DenseVector (datarow);
            
            KdTreeNode<float, int>[] result = tree.GetNearestNeighbours(datarow, nn);
            indices.Clear ();
            dists.Clear ();
            for (int a = 0; a < nn; a++)
            {
                indices.Add (result[a].Value);
                dists.Add (sqrnorm ((Vector) (new DenseVector (result[a].Point) - datarowv))); 
            }
        }
        
        private List <T> buildList<T> (int size, T initialValue)
        {
            List <T> l = new List <T> ();
            for (int i = 0; i < size; i++)
            {
                l.Add (initialValue);
            }
            return l;
        }

        public void AdvancedMatching()
        {
            int fi = 0;
            int fj = 1;
            
            Debug.LogFormat("Advanced matching : [{0} - {1}]\n", fi, fj);
            bool swapped = false;
            
            if (pointcloud_[fj].Count > pointcloud_[fi].Count)
            {
                int temp = fi;
                fi = fj;
                fj = temp;
                swapped = true;
            }
            
            int nPti = pointcloud_[fi].Count;
            int nPtj = pointcloud_[fj].Count;
            
            KdTree<float, int> feature_tree_i = new KdTree<float, int> (features_[fi][0].Count, new FloatMath());
            BuildKDTree(features_[fi], feature_tree_i);
            
            KdTree<float, int> feature_tree_j = new KdTree<float, int> (features_[fj][0].Count, new FloatMath());
            BuildKDTree(features_[fj], feature_tree_j);
            
            bool crosscheck = true;
            bool tuple = true;
            
            List<int> corres_K = new List<int> ();
            List<int> corres_K2 = new List<int> (); 
            List<float> dis = new List<float> ();
            List<int> ind = new List<int> ();
            
            List<(int, int)> corres = new List<(int, int)> ();
            List<(int, int)> corres_cross = new List<(int, int)> ();
            List<(int, int)> corres_ij = new List<(int, int)> ();
            List<(int, int)> corres_ji = new List<(int, int)> ();
            
            ///////////////////////////
            /// INITIAL MATCHING
            ///////////////////////////
            
            List<int> i_to_j = buildList (nPti, -1);
            for (int j = 0; j < nPtj; j++)
            {
                SearchKDTree(feature_tree_i, features_[fj][j], corres_K, dis, 1);
                int i = corres_K[0];
                if (i_to_j[i] == -1)
                {
                    SearchKDTree(feature_tree_j, features_[fi][i], corres_K, dis, 1);
                    int ij = corres_K[0];
                    i_to_j[i] = ij;
                }
                corres_ji.Add((i, j));
            }
            
            for (int i = 0; i < nPti; i++)
            {
                if (i_to_j[i] != -1)
                    corres_ij.Add((i, i_to_j[i]));
            }
            
            int ncorres_ij = corres_ij.Count;
            int ncorres_ji = corres_ji.Count;
            
            // corres = corres_ij + corres_ji;
            for (int i = 0; i < ncorres_ij; ++i)
                corres.Add(corres_ij[i]);
            for (int j = 0; j < ncorres_ji; ++j)
                corres.Add(corres_ji[j]);
            
            Debug.LogFormat("Number of points that remain: {0}\n", (int)corres.Count);
            
            ///////////////////////////
            /// CROSS CHECK
            /// input : corres_ij, corres_ji
            /// output : corres
            ///////////////////////////
            if (crosscheck)
            {
                Debug.LogFormat("\t[cross check] ");
                
                // build data structure for cross check
                corres.Clear();
                corres_cross.Clear();
                List<List<int> > Mi = new List<List<int> > (nPti);
                List<List<int> > Mj= new List<List<int> > (nPtj);
                
                for (int i = 0; i < nPti; ++i)
                {
                    Mi.Add (new List <int> ());
                }               
                for (int j = 0; j < nPtj; ++j)
                {
                    Mj.Add (new List <int> ());
                }               

                int ci, cj;
                for (int i = 0; i < ncorres_ij; ++i)
                {
                    ci = corres_ij[i].Item1;
                    cj = corres_ij[i].Item2;
                    Mi[ci].Add(cj);
                }
                for (int j = 0; j < ncorres_ji; ++j)
                {
                    ci = corres_ji[j].Item1;
                    cj = corres_ji[j].Item2;
                    Mj[cj].Add(ci);
                }
                
                // cross check
                for (int i = 0; i < nPti; ++i)
                {
                    for (int ii = 0; ii < Mi[i].Count; ++ii)
                    {
                        int j = Mi[i][ii];
                        for (int jj = 0; jj < Mj[j].Count; ++jj)
                        {
                            if (Mj[j][jj] == i)
                            {
                                corres.Add((i, j));
                                corres_cross.Add((i, j));
                            }
                        }
                    }
                }
                Debug.LogFormat("Number of points that remain after cross-check: {0}\n", (int)corres.Count);
            }
            
            ///////////////////////////
            /// TUPLE CONSTRAINT
            /// input : corres
            /// output : corres
            ///////////////////////////
            if (tuple)
            {
                UnityEngine.Random.InitState ((int) System.DateTime.Now.Ticks);
                
                Debug.LogFormat("\t[tuple constraint] ");
                int rand0, rand1, rand2;
                int idi0, idi1, idi2;
                int idj0, idj1, idj2;
                float scale = tuple_scale_;
                int ncorr = corres.Count;
                int number_of_trial = ncorr * 100;
                List<(int, int)> corres_tuple = new List<(int, int)> ();
                
                int cnt = 0;
                int i;
                for (i = 0; i < number_of_trial; i++)
                {
                    rand0 = UnityEngine.Random.Range (0, ncorr);
                    rand1 = UnityEngine.Random.Range (0, ncorr);
                    rand2 = UnityEngine.Random.Range (0, ncorr);
                    
                    idi0 = corres[rand0].Item1;
                    idj0 = corres[rand0].Item2;
                    idi1 = corres[rand1].Item1;
                    idj1 = corres[rand1].Item2;
                    idi2 = corres[rand2].Item1;
                    idj2 = corres[rand2].Item2;
                    
                    // collect 3 points from i-th fragment
                    Vector pti0 = pointcloud_[fi][idi0];
                    Vector pti1 = pointcloud_[fi][idi1];
                    Vector pti2 = pointcloud_[fi][idi2];
                    
                    float li0 = norm ((Vector) (pti0 - pti1));
                    float li1 = norm ((Vector) (pti1 - pti2));
                    float li2 = norm ((Vector) (pti2 - pti0));
                    
                    // collect 3 points from j-th fragment
                    Vector ptj0 = pointcloud_[fj][idj0];
                    Vector ptj1 = pointcloud_[fj][idj1];
                    Vector ptj2 = pointcloud_[fj][idj2];
                    
                    float lj0 = norm ((Vector) (ptj0 - ptj1));
                    float lj1 = norm ((Vector) (ptj1 - ptj2));
                    float lj2 = norm ((Vector) (ptj2 - ptj0));
                    
                    if ((li0 * scale < lj0) && (lj0 < li0 / scale) &&
                        (li1 * scale < lj1) && (lj1 < li1 / scale) &&
                        (li2 * scale < lj2) && (lj2 < li2 / scale))
                    {
                        corres_tuple.Add((idi0, idj0));
                        corres_tuple.Add((idi1, idj1));
                        corres_tuple.Add((idi2, idj2));
                        cnt++;
                    }
                    
                    if (cnt >= tuple_max_cnt_)
                        break;
                }
                
                Debug.LogFormat("{0} tuples ({1} trial, {2} actual).\n", cnt, number_of_trial, i);
                corres.Clear();
                
                for (i = 0; i < corres_tuple.Count; ++i)
                    corres.Add((corres_tuple[i].Item1, corres_tuple[i].Item2));
            }
            
            if (swapped)
            {
                List<(int, int)> temp = new List<(int, int)> ();
                for (int i = 0; i < corres.Count; i++)
                    temp.Add((corres[i].Item2, corres[i].Item1));
                corres.Clear();
                corres = temp;
            }
            
            Debug.LogFormat("\t[final] matches {0}.\n", (int)corres.Count);
            corres_ = corres;
        }
        
        // Normalize scale of points.
        // X' = (X-\mu)/scale
        public void NormalizePoints()
        {
            int num = 2;
            float scale = 0;
            
            Means.Clear();
            
            for (int i = 0; i < num; ++i)
            {
                float max_scale = 0;
                
                // compute mean
                Vector mean = new DenseVector (3);
                mean.Clear();
                
                int npti = pointcloud_[i].Count;
                for (int ii = 0; ii < npti; ++ii)
                {
                    Vector p = (Vector) new DenseVector (new float [] {pointcloud_[i][ii][0], pointcloud_[i][ii][1], pointcloud_[i][ii][2]});
                    mean = (Vector) (mean + p);
                }
                mean = (Vector) (mean / npti);
                Means.Add(mean);
                
                Debug.LogFormat ("normalize points :: mean[{0}] = [{1} {2} {3}]\n", i, mean[0], mean[1], mean[2]);
                
                for (int ii = 0; ii < npti; ++ii)
                {
                    pointcloud_[i][ii][0] -= mean[0];
                    pointcloud_[i][ii][1] -= mean[1];
                    pointcloud_[i][ii][2] -= mean[2];
                }
                
                // compute scale
                for (int ii = 0; ii < npti; ++ii)
                {
                    Vector p = (Vector) new DenseVector (new float [] {pointcloud_[i][ii][0], pointcloud_[i][ii][1], pointcloud_[i][ii][2]});
                    float temp = norm (p); // because we extract mean in the previous stage.
                    if (temp > max_scale)
                        max_scale = temp;
                }
                
                if (max_scale > scale)
                    scale = max_scale;
            }
            
            //// mean of the scale variation
            if (use_absolute_scale_) {
                GlobalScale = 1.0f;
                StartScale = scale;
            } else {
                GlobalScale = scale; // second choice: we keep the maximum scale.
                StartScale = 1.0f;
            }
            Debug.LogFormat("normalize points :: global scale : {0}\n", GlobalScale);
            
            for (int i = 0; i < num; ++i)
            {
                int npti = pointcloud_[i].Count;
                for (int ii = 0; ii < npti; ++ii)
                {
                    pointcloud_[i][ii][0] /= GlobalScale;
                    pointcloud_[i][ii][1] /= GlobalScale;
                    pointcloud_[i][ii][2] /= GlobalScale;
                }
            }
        }
        
        public double OptimizePairwise(bool decrease_mu_)
        {
            Debug.LogFormat("Pairwise rigid pose optimization\n");
            
            double par;
            int numIter = iteration_number_;
            TransOutput_ = Matrix4x4.identity;
            
            par = StartScale; // mu
            
            int i = 0;
            int j = 1;
            
            // make another copy of pointcloud_[j].
            Points pcj_copy = new Points ();
            int npcj = pointcloud_[j].Count;
            for (int cnt = 0; cnt < npcj; cnt++)
                pcj_copy.Add (pointcloud_[j][cnt]);
            
            if (corres_.Count < 10)
                return -1;
            
            List<double> s = buildList (corres_.Count, 1.0);
            
            Matrix4x4 trans = Matrix4x4.identity;
            
            for (int itr = 0; itr < numIter; itr++) {
                
                // graduated non-convexity.
                if (decrease_mu_)
                {
                    if (itr % 4 == 0 && par > max_corr_dist_) {
                        par /= div_factor_;
                    }
                }
                
                const int nvariable = 6;	// 3 for rotation and 3 for translation
                Matrix JTJ = (Matrix) Matrix.Build.Dense (nvariable, nvariable, 0.0f);
                Matrix JTr = (Matrix) Matrix.Build.Dense (nvariable, 1, 0.0f);
                Matrix J;
                
                double r;
                double r2 = 0.0;
                
                for (int c = 0; c < corres_.Count; c++) {
                    int ii = corres_[c].Item1;
                    int jj = corres_[c].Item2;
                    Vector p, q;
                    p = pointcloud_[i][ii];
                    q = pcj_copy[jj];
                    Vector rpq = (Vector) (p - q);
                    
                    int c2 = c;
                    
                    float temp = (float) (par / (rpq.DotProduct(rpq) + par));
                    s[c2] = temp * temp;
                    
                    J = (Matrix) Matrix.Build.Dense (nvariable, 1, 0.0f);
                    J[1, 0] = -q[2];
                    J[2, 0] = q[1];
                    J[3, 0] = -1;
                    r = rpq[0];
                    JTJ = (Matrix) (JTJ + J * J.Transpose() * (float) s[c2]);
                    JTr = (Matrix) (JTr + J * (float) (r * s[c2]));
                    r2 += r * r * s[c2];
                    
                    J = (Matrix) Matrix.Build.Dense (nvariable, 1, 0.0f);
                    J[2, 0] = -q[0];
                    J[0, 0] = q[2];
                    J[4, 0] = -1;
                    r = rpq[1];
                    JTJ = (Matrix) (JTJ + J * J.Transpose() * (float) s[c2]);
                    JTr = (Matrix) (JTr + J * (float) (r * s[c2]));
                    r2 += r * r * s[c2];
                    
                    J = (Matrix) Matrix.Build.Dense (nvariable, 1, 0.0f);
                    J[0, 0] = -q[1];
                    J[1, 0] = q[0];
                    J[5, 0] = -1;
                    r = rpq[2];
                    JTJ = (Matrix) (JTJ + J * J.Transpose() * (float) s[c2]);
                    JTr = (Matrix) (JTr + J * (float) (r * s[c2]));
                    r2 += r * r * s[c2];
                    
                    r2 += (par * (1.0 - Math.Sqrt(s[c2])) * (1.0 - Math.Sqrt(s[c2])));
                }
                
                Matrix result;
                result = (Matrix) (-JTJ.Solve(JTr)); // Removed LLT
                
                Matrix4x4 aff_mat = new Matrix4x4 ();
                aff_mat.SetTRS (new Vector3 (result[3, 0], result[4, 0], result[5, 0]), Quaternion.AngleAxis (result[2, 0] * Mathf.Rad2Deg, Vector3.forward) * Quaternion.AngleAxis (result[1, 0] * Mathf.Rad2Deg, Vector3.up) * Quaternion.AngleAxis (result[0, 0] * Mathf.Rad2Deg, Vector3.right), Vector3.one);
                Matrix4x4 delta = aff_mat;
                
                trans = delta * trans;
                TransformPoints(pcj_copy, delta);
                
            }
            
            TransOutput_ = trans * TransOutput_;
            return par;
        }
       
        private void TransformPoints(Points points, Matrix4x4 Trans)
        {
            int npc = (int)points.Count;
            Vector3 temp;
            for (int cnt = 0; cnt < npc; cnt++) {
                temp = Trans.MultiplyPoint (new Vector3 (points[cnt][0], points[cnt][1], points[cnt][2]));
                points[cnt] = new DenseVector (new float [] { temp.x, temp.y,temp.z });
            }
        }
        
        public List <Vector3> GetTransformedPoints (List <Vector3> src)
        {
            Points pcj_copy = new Points ();
            int npcj = src.Count;
            for (int cnt = 0; cnt < npcj; cnt++)
                pcj_copy.Add (new DenseVector (new float [] { src[cnt].x, src[cnt].y, src[cnt].z }));
            TransformPoints(pcj_copy, GetOutputTrans ());
            
            List <Vector3> v = new List <Vector3> ();
            for (int cnt = 0; cnt < npcj; cnt++)
            {
                v.Add (new Vector3 (pcj_copy[cnt][0], pcj_copy[cnt][1], pcj_copy[cnt][2]));
            }
            return v;
        }
        
        public Matrix4x4 GetOutputTrans()
        {
//             Debug.Log ("Res " + TransOutput_ + " " + Means.Count);
            Quaternion R = TransOutput_.rotation;
            Vector3 t = new Vector3 (TransOutput_[0, 3], TransOutput_[1, 3], TransOutput_[2, 3]);
            Matrix4x4 transtemp = TransOutput_;
            Vector3 tt = -(R* new Vector3 (Means[1][0], Means[1][1], Means[1][2])) + t*GlobalScale + new Vector3 (Means[0][0], Means[0][1], Means[0][2]);
            transtemp[0, 3] = tt.x;
            transtemp[1, 3] = tt.y;
            transtemp[2, 3] = tt.z;
            
            return transtemp;
        }
        
        public void WriteTrans(string filepath)
        {
            StreamWriter fid = new StreamWriter (filepath);
            
            // Below line indicates how the transformation matrix aligns two point clouds
            // e.g. T * pointcloud_[1] is aligned with pointcloud_[0].
            // '2' indicates that there are two point cloud fragments.
            fid.Write ("0 1 2\n");
            
            Matrix4x4 transtemp = GetOutputTrans();
            
            fid.Write (string.Format ("{0:0.0000000000} {1:0.0000000000} {2:0.0000000000} {3:0.0000000000}\n", transtemp[0, 0], transtemp[0, 1], transtemp[0, 2], transtemp[0, 3]));
            fid.Write (string.Format ("{0:0.0000000000} {1:0.0000000000} {2:0.0000000000} {3:0.0000000000}\n", transtemp[1, 0], transtemp[1, 1], transtemp[1, 2], transtemp[1, 3]));
            fid.Write (string.Format ("{0:0.0000000000} {1:0.0000000000} {2:0.0000000000} {3:0.0000000000}\n", transtemp[2, 0], transtemp[2, 1], transtemp[2, 2], transtemp[2, 3]));
            fid.Write (string.Format ("{0:0.0000000000} {1:0.0000000000} {2:0.0000000000} {3:0.0000000000}\n", transtemp[3, 0], transtemp[3, 1], transtemp[3, 2], transtemp[3, 3]));
            
            fid.Close();
        }
        
        private int [] parseInt (string line)
        {
            string [] v = line.Split ();
            int [] result = new int [v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                result[i] = int.Parse (v[i]);
            }
            return result;
        }
        
        private float [] parseFloat (string line)
        {
            string [] v = line.Split ();
            float [] result = new float [v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                result[i] = float.Parse (v[i]);
            }
            return result;
        }
        
        private Matrix4x4 ReadTrans(string filename)
        {
            Matrix4x4 temp = new Matrix4x4 ();
            int [] tempi;
            int cnt = 0;
            StreamReader fid = new StreamReader (filename);
            if ((tempi = parseInt (fid.ReadLine ())).Length == 3)
            {
                for (int j = 0; j < 4; j++)
                {
                    float [] a;
                    a = parseFloat (fid.ReadLine ());
                    temp[j, 0] = a[0];
                    temp[j, 1] = a[1];
                    temp[j, 2] = a[2];
                    temp[j, 3] = a[3];
                }
            }
            return temp;
        }
        
        private Points copyPoints (Points p)
        {
            Points r = new Points ();
            for (int i = 0; i < p.Count; i++)
            {
                r.Add (p[i]);
            }
            return r;
        }
        
        private void BuildDenseCorrespondence(Matrix4x4 trans, 
                                            Correspondences corres)
        {   
            int fi = 0;
            int fj = 1;
            Points pci = pointcloud_[fi];
            Points pcj = copyPoints (pointcloud_[fj]);
            TransformPoints(pcj, trans);
            
            KdTree<float, int> feature_tree_i = new KdTree<float, int> (pci[0].Count, new FloatMath());
            BuildKDTree(pci, feature_tree_i);
            
            List<int> ind = new List <int> ();
            List<float> dist = new List <float> ();
            corres.Clear();
            for (int j = 0; j < pcj.Count; ++j)
            {
                SearchKDTree(feature_tree_i, pcj[j], ind, dist, 1);
                float dist_j = Mathf.Sqrt(dist[0]);
                if (dist_j / GlobalScale < max_corr_dist_ / 2.0)
                    corres.Add((ind[0], j));
            }
        }
        
        public void Evaluation(string gth, string estimation, string output)
        {
            float inlier_ratio = -1.0f;
            float overlapping_ratio = -1.0f;
            
            int fi = 0;
            int fj = 1;
            
            Correspondences corres = new Correspondences ();
            Matrix4x4 gth_trans = ReadTrans(gth);
            BuildDenseCorrespondence(gth_trans, corres);
            Debug.LogFormat("Groundtruth correspondences [{0}-{1}] : {2}\n", fi, fj, 
                   (int)corres.Count);
            
            int ncorres = corres.Count;
            float err_mean = 0.0f;
            
            Points pci = pointcloud_[fi];
            Points pcj = pointcloud_[fj];
            Matrix4x4 est_trans = ReadTrans(estimation);
            List<float> error = new List <float> ();
            for (int i = 0; i < ncorres; ++i)
            {
                int idi = corres[i].Item1;
                int idj = corres[i].Item2;
                Vector3 pi = new Vector3 (pci[idi][0], pci[idi][1], pci[idi][2]);
                Vector3 pj = new Vector3 (pcj[idj][0], pcj[idj][1], pcj[idj][2]);
                Vector3 pjt = est_trans.MultiplyPoint (pj);
                float errtemp = (pi - pjt).magnitude;
                error.Add(errtemp);
                // this is based on the RMSE defined in
                // https://en.wikipedia.org/wiki/Root-mean-square_deviation
                errtemp = errtemp * errtemp;
                err_mean += errtemp;
            }
            err_mean /= ncorres; // this is MSE = mean(d^2)
            err_mean = Mathf.Sqrt(err_mean); // this is RMSE = sqrt(MSE)
            Debug.LogFormat("mean error : {0:0.0000E+0}\n", err_mean);
            
            //overlapping_ratio = (float)ncorres / min(
            //		pointcloud_[fj].Count, pointcloud_[fj].Count);
            overlapping_ratio = (float)ncorres / pointcloud_[fj].Count;
            
            // write errors
            StreamWriter fid = new StreamWriter (output);
            fid.Write (string.Format ("{0} {1} {2} {3} {4}\n", fi, fj, err_mean, 
                    inlier_ratio, overlapping_ratio));
            fid.Close();
        }        
    }    
}
